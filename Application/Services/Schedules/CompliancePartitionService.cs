// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ICompliancePartitionService"/>. Runs one batched pre-commit check over all
/// rows; when nothing blocks, everything is accepted. When only Block-mode compliance escalations
/// block and the caller is an authorized supervisor requesting an override, everything is accepted
/// with an audit log. Otherwise rows of clients without an Error in the batch result are accepted
/// wholesale and only the violating clients' rows are re-tested greedily one by one (deterministic
/// client/date/start order), so a single bad row never drops its client's clean rows.
/// </summary>
/// <param name="conflictChecker">Pre-commit guardrail used for the batch and per-row trial checks</param>
/// <param name="overrideAuthorizer">Resolves whether the caller may override a Block-mode escalation (K1)</param>
/// <param name="httpContextAccessor">Resolves the caller's name for the override audit log</param>
/// <param name="logger">Logs a structured audit entry whenever a supervisor override is applied</param>
using System.Security.Claims;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class CompliancePartitionService : ICompliancePartitionService
{
    private const string UnknownUserName = "Unknown";

    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CompliancePartitionService> _logger;

    public CompliancePartitionService(
        IPreCommitConflictChecker conflictChecker,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CompliancePartitionService> logger)
    {
        _conflictChecker = conflictChecker;
        _overrideAuthorizer = overrideAuthorizer;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<CompliancePartitionResult> PartitionAsync(
        IReadOnlyList<PlannedWorkRow> rows,
        Guid? analyseToken,
        bool overrideBlockRequested,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return new CompliancePartitionResult([], [], [], OverrideApplied: false);
        }

        var batch = await _conflictChecker.CheckAsync(rows, analyseToken, cancellationToken);
        if (!batch.HasBlocking)
        {
            return new CompliancePartitionResult(AllIndexes(rows.Count), [], Reportable(batch), OverrideApplied: false);
        }

        var authorized = await _overrideAuthorizer.IsAuthorizedAsync(overrideBlockRequested);
        if (CanOverride(batch, authorized))
        {
            LogOverride(batch, rows.Count);
            return new CompliancePartitionResult(AllIndexes(rows.Count), [], Reportable(batch), OverrideApplied: true);
        }

        return await PartitionGreedilyAsync(rows, batch, analyseToken, authorized, cancellationToken);
    }

    public async Task<OptionPartitionResult> PartitionOptionsAsync(
        IReadOnlyList<PlannedOption> options,
        Guid? analyseToken,
        bool overrideBlockRequested,
        CancellationToken cancellationToken)
    {
        if (options.Count == 0)
        {
            return new OptionPartitionResult([], [], [], OverrideApplied: false);
        }

        var allRows = options.SelectMany(o => o.Rows).ToList();
        var allRemovals = options.SelectMany(o => o.Removals).ToList();

        var batch = await _conflictChecker.CheckAsync(allRows, allRemovals, analyseToken, cancellationToken);
        if (!batch.HasBlocking)
        {
            return new OptionPartitionResult(AllIndexes(options.Count), [], Reportable(batch), OverrideApplied: false);
        }

        var authorized = await _overrideAuthorizer.IsAuthorizedAsync(overrideBlockRequested);
        if (CanOverride(batch, authorized))
        {
            LogOverride(batch, options.Count);
            return new OptionPartitionResult(AllIndexes(options.Count), [], Reportable(batch), OverrideApplied: true);
        }

        return await PartitionOptionsGreedilyAsync(options, batch, analyseToken, authorized, cancellationToken);
    }

    /// <summary>
    /// Greedy fallback per OPTION rather than per row: an option is accepted or refused as a whole, so a
    /// swap can never be half-applied. Options whose clients contributed no error are taken wholesale;
    /// the rest are tried in submission order against the growing accepted set of their clients.
    /// </summary>
    private async Task<OptionPartitionResult> PartitionOptionsGreedilyAsync(
        IReadOnlyList<PlannedOption> options,
        PreCommitCheckResult batch,
        Guid? analyseToken,
        bool authorized,
        CancellationToken cancellationToken)
    {
        var violatingClients = batch.NewConflicts
            .Where(c => c.Type == ScheduleValidationType.Error)
            .Select(c => c.ClientId)
            .ToHashSet();

        var accepted = new List<int>();
        var blocked = new List<BlockedOption>();
        var reportable = batch.NewConflicts
            .Where(c => !violatingClients.Contains(c.ClientId))
            .ToList();
        var overrideApplied = false;

        var acceptedRows = new List<PlannedWorkRow>();
        var acceptedRemovals = new List<PlannedRemovalRow>();
        var lastCheck = new List<PreCommitCheckResult>();

        for (var index = 0; index < options.Count; index++)
        {
            var option = options[index];
            var clients = option.Rows.Select(r => r.ClientId)
                .Concat(option.Removals.Select(r => r.ClientId))
                .ToHashSet();

            if (!clients.Overlaps(violatingClients))
            {
                accepted.Add(index);
                continue;
            }

            var trialRows = acceptedRows.Where(r => clients.Contains(r.ClientId)).Concat(option.Rows).ToList();
            var trialRemovals = acceptedRemovals.Where(r => clients.Contains(r.ClientId)).Concat(option.Removals).ToList();

            var check = await _conflictChecker.CheckAsync(trialRows, trialRemovals, analyseToken, cancellationToken);
            if (check.HasBlocking && !CanOverride(check, authorized))
            {
                var reason = check.NewConflicts.First(c => c.Type == ScheduleValidationType.Error).Comment;
                blocked.Add(new BlockedOption(index, reason));
                continue;
            }

            if (check.HasBlocking)
            {
                LogOverride(check, 1);
                overrideApplied = true;
            }

            acceptedRows.AddRange(option.Rows);
            acceptedRemovals.AddRange(option.Removals);
            accepted.Add(index);
            lastCheck.Add(check);
        }

        reportable.AddRange(lastCheck.SelectMany(Reportable));

        return new OptionPartitionResult(accepted, blocked, reportable, overrideApplied);
    }

    /// <summary>
    /// Greedy fallback once the batch is known to block without a batch-level override: clients that
    /// contributed no Error to the batch result are accepted wholesale (their warnings come from the
    /// batch result); only the violating clients' rows are re-tested one by one against the growing
    /// accepted set of that client, so exactly the rows that flip the check to Error are blocked.
    /// A per-row check that blocks only via Block-mode escalations may still be overridden.
    /// </summary>
    private async Task<CompliancePartitionResult> PartitionGreedilyAsync(
        IReadOnlyList<PlannedWorkRow> rows,
        PreCommitCheckResult batch,
        Guid? analyseToken,
        bool authorized,
        CancellationToken cancellationToken)
    {
        var violatingClients = batch.NewConflicts
            .Where(c => c.Type == ScheduleValidationType.Error)
            .Select(c => c.ClientId)
            .ToHashSet();

        var accepted = new List<int>();
        var blocked = new List<BlockedRow>();
        var reportable = batch.NewConflicts
            .Where(c => !violatingClients.Contains(c.ClientId))
            .ToList();
        var overrideApplied = false;

        var acceptedRowsByClient = new Dictionary<Guid, List<PlannedWorkRow>>();
        var lastCheckByClient = new Dictionary<Guid, PreCommitCheckResult>();

        foreach (var (row, index) in EnumerateDeterministically(rows))
        {
            if (!violatingClients.Contains(row.ClientId))
            {
                accepted.Add(index);
                continue;
            }

            if (!acceptedRowsByClient.TryGetValue(row.ClientId, out var clientRows))
            {
                clientRows = [];
                acceptedRowsByClient[row.ClientId] = clientRows;
            }

            var trial = new List<PlannedWorkRow>(clientRows) { row };
            var check = await _conflictChecker.CheckAsync(trial, analyseToken, cancellationToken);
            if (check.HasBlocking && !CanOverride(check, authorized))
            {
                var reason = check.NewConflicts.First(c => c.Type == ScheduleValidationType.Error).Comment;
                blocked.Add(new BlockedRow(index, reason));
                continue;
            }

            if (check.HasBlocking)
            {
                LogOverride(check, 1);
                overrideApplied = true;
            }

            clientRows.Add(row);
            accepted.Add(index);
            lastCheckByClient[row.ClientId] = check;
        }

        reportable.AddRange(lastCheckByClient.Values.SelectMany(Reportable));
        accepted.Sort();
        var orderedBlocked = blocked.OrderBy(b => b.Index).ToList();

        return new CompliancePartitionResult(accepted, orderedBlocked, reportable, overrideApplied);
    }

    private static IEnumerable<(PlannedWorkRow Row, int Index)> EnumerateDeterministically(
        IReadOnlyList<PlannedWorkRow> rows)
        => rows
            .Select((row, index) => (Row: row, Index: index))
            .OrderBy(x => x.Row.ClientId)
            .ThenBy(x => x.Row.Date)
            .ThenBy(x => x.Row.StartTime)
            .ThenBy(x => x.Index);

    /// <summary>
    /// K1 supervisor override: only ever applies to a check whose Error entries are exclusively
    /// Block-mode compliance escalations. A structural Error (collision, missing mandatory
    /// qualification) is never overridable, by construction, regardless of role or request flag.
    /// </summary>
    private static bool CanOverride(PreCommitCheckResult check, bool authorized)
        => authorized && !check.HasHardBlocking && check.HasOverridableBlocking;

    // Everything an accepted check reports: warnings plus any Error entry that was overridden rather
    // than blocked (a structural Error never reaches an accepted check, by construction).
    private static List<ScheduleValidationNotificationDto> Reportable(PreCommitCheckResult result)
        => result.NewConflicts
            .Where(c => c.Type != ScheduleValidationType.Error
                || c.CommentParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey))
            .ToList();

    private static List<int> AllIndexes(int count) => Enumerable.Range(0, count).ToList();

    private void LogOverride(PreCommitCheckResult check, int rowCount)
    {
        var overriddenRules = check.NewConflicts
            .Where(c => c.Type == ScheduleValidationType.Error
                && c.CommentParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey))
            .Select(c => c.CommentParams[ComplianceRuleNames.EnforcementRuleParamKey])
            .Distinct()
            .ToList();
        var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? UnknownUserName;

        _logger.LogWarning(
            "Compliance override: user {User} overrode {RuleCount} blocked rule(s) ({Rules}) to accept {RowCount} planned row(s).",
            userName,
            overriddenRules.Count,
            string.Join(",", overriddenRules),
            rowCount);
    }
}
