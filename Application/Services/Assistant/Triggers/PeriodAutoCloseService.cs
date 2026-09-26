// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IPeriodAutoCloseService: Klacksy closes a group's period on its own. Scoped and called from
/// PeriodAutoCloseDetector inside the hourly AgentTriggerBackgroundService tick - the same place and cadence
/// as the next-period automation - and the tick reports the returned events through the condition ledger.
/// A close is irreversible in effect: reopening does not restore confirmations, and the group-scoped
/// PeriodClosedEvent the close handler raises after its commit triggers the payroll export
/// (PayrollExportOnPeriodClosedHandler, idempotent per group, target system and period). Hence the order of
/// gates, per group, each of which ends the evaluation of that group:
///
/// 1. Individual groups (no derivable cycle), unstaffed groups and periods ending before the group's ValidFrom
///    are skipped silently. The candidate is ONE period per group: the latest one whose close date has come
///    (PeriodBoundaries.LastEndBefore on PeriodCloseDateCalculator.DuePeriodEndBound). Never a global close.
/// 2. IPeriodAutoCloseResolver must allow the close for this group (kill switch off; the period_auto_close
///    rule enabled at Execute; global level and admin minimum FullyAutonomous). Otherwise nothing happens and
///    nothing is reported - automatic closing is simply not switched on, and the reminders remain.
/// 3. A period whose last day is sealed (group or installation-wide lock) is skipped silently: it is closed, a
///    second close would re-run the payroll export. A period without work on the group's OWN shifts is skipped
///    silently too: the seal, the day locks and the payroll export act on the direct GroupItem of the group
///    only, so a parent group whose work hangs on child groups would be "closed" with nothing sealed - the
///    child groups are closed on their own. A period without any work at all would recreate the
///    unplanned-period noise PeriodOverdueDetector documents. A period with an UNSEAL entry in its
///    PeriodAuditLog (this group or installation-wide) is skipped silently as well: the automatic path never
///    re-closes a period a person reopened on purpose.
/// From here on every non-close is reported as a PeriodAutoCloseBlockedTriggerEvent with its cause:
/// 4. No PERIOD_CLOSE_LAG_DAYS stored: never close - the stored value is the proof that the user was asked.
/// 5. Outside PeriodAutoClose.WindowDays after the first allowed day: left to a person.
/// 6. Any day of the period already sealed (group or installation-wide lock, but not the last day): left to a
///    person and reported as PartiallySealed.
/// 7. In a FRESH DI scope, directly before the seal: the autonomy decision and the sealed state are read
///    again, and the period issues are loaded (GetPeriodIssuesQuery, the source of list_period_issues) - any
///    error blocks. Then ClosePeriodByGroupCommand runs with the deciding admin as ActingAdminUserId (there
///    is no HttpContext in the tick) and a constant reason, never with AcknowledgeViolations, so the
///    handler's own error check stays fail-closed as well. The fresh scope keeps a failed close - and the
///    issue loader, which refreshes materialised rest-obligation state - from leaving staged rows in the
///    tick's shared DbContext, where the next ledger write would flush them.
/// 8. The fresh read-back must show every day of the period sealed; only then is the close reported.
/// A close that throws unexpectedly is re-read in another fresh scope (ResolveFailedCloseAsync): a concurrent
/// close by a person is recognised and stays silent instead of being reported as a failure.
/// A failure in one group is caught, logged and reported for that group and never stops the others.
/// Not applied: the DailyActionBudget/WindowActionLimit of the governance rule - one scan may close every
/// armed group whose period is due. The per-group gates above are the brake, not a rate limit.
/// A reported close proves the seal (read back), NOT the payroll export: the close handler dispatches the
/// PeriodClosedEvent after its commit and only logs a failing hook.
/// </summary>
/// <param name="groupRepository">Lists the groups and which of them have members.</param>
/// <param name="weekConfiguration">Resolves the configured week start for weekly periods.</param>
/// <param name="settingsReader">Reads the stored close lag.</param>
/// <param name="companyClock">Today as the company's local day.</param>
/// <param name="autonomyResolver">Autonomy gates for the evaluation (re-resolved in the fresh scope before the seal).</param>
/// <param name="sealedDayRepository">Sealed state of the period.</param>
/// <param name="activityProbe">Whether the period holds work on the group's own shifts.</param>
/// <param name="auditLogRepository">Reopen history of the period.</param>
/// <param name="scopeFactory">Fresh scope for the re-checks (autonomy, sealed state, period issues), the seal and the read-back.</param>
/// <param name="logger">Structured log per group and per run.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using InvalidRequestException = Klacks.Api.Domain.Exceptions.InvalidRequestException;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class PeriodAutoCloseService : IPeriodAutoCloseService
{
    private const int NoErrors = 0;

    private readonly IGroupRepository _groupRepository;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly ISettingsReader _settingsReader;
    private readonly ICompanyClock _companyClock;
    private readonly IPeriodAutoCloseResolver _autonomyResolver;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PeriodAutoCloseService> _logger;

    public PeriodAutoCloseService(
        IGroupRepository groupRepository,
        IWeekConfiguration weekConfiguration,
        ISettingsReader settingsReader,
        ICompanyClock companyClock,
        IPeriodAutoCloseResolver autonomyResolver,
        ISealedDayRepository sealedDayRepository,
        IScheduleActivityProbe activityProbe,
        IPeriodAuditLogRepository auditLogRepository,
        IServiceScopeFactory scopeFactory,
        ILogger<PeriodAutoCloseService> logger)
    {
        _groupRepository = groupRepository;
        _weekConfiguration = weekConfiguration;
        _settingsReader = settingsReader;
        _companyClock = companyClock;
        _autonomyResolver = autonomyResolver;
        _sealedDayRepository = sealedDayRepository;
        _activityProbe = activityProbe;
        _auditLogRepository = auditLogRepository;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IAgentTriggerEvent>> RunAsync(CancellationToken cancellationToken = default)
    {
        var groups = await _groupRepository.List();
        if (groups.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var lagDays = await PeriodCloseLagReader.ReadAsync(_settingsReader);
        var referenceDay = PeriodCloseDateCalculator.DuePeriodEndBound(today, lagDays ?? 0);
        var referenceWeekStart = await _weekConfiguration.GetWeekStartAsync(referenceDay, cancellationToken);
        var staffing = GroupStaffingLookup.Build(
            groups,
            await _groupRepository.GetGroupIdsWithMembersAsync(cancellationToken));

        var events = new List<IAgentTriggerEvent>();
        var armedGroups = 0;
        foreach (var group in groups)
        {
            if (!NextPeriodBoundaries.HasDerivableCycle(group.PaymentInterval) || !staffing.IsStaffed(group.Id))
            {
                continue;
            }

            var periodEnd = PeriodBoundaries.LastEndBefore(group, referenceDay, referenceWeekStart);
            if (periodEnd < DateOnly.FromDateTime(group.ValidFrom))
            {
                continue;
            }

            var periodStart = PeriodBoundaries.StartFor(group.PaymentInterval, periodEnd);
            var candidate = new Candidate(group, periodStart, periodEnd);

            var decision = await TryResolveAsync(candidate, cancellationToken);
            if (decision is not { CanClose: true })
            {
                continue;
            }

            armedGroups++;
            var triggerEvent = await EvaluateArmedGroupAsync(candidate, today, lagDays, cancellationToken);
            if (triggerEvent != null)
            {
                events.Add(triggerEvent);
            }
        }

        _logger.LogInformation(
            "PeriodAutoClose scan: {Total} group(s), {Armed} with automatic closing allowed, {Closed} closed, {Blocked} blocked",
            groups.Count,
            armedGroups,
            events.Count(e => e is PeriodAutoClosedTriggerEvent),
            events.Count(e => e is PeriodAutoCloseBlockedTriggerEvent));

        return events;
    }

    /// <summary>
    /// The autonomy decision for the group, or null when resolving it failed. A failure is logged but never
    /// reported to the planners: whether automatic closing is switched on at all is unknown then, and a
    /// "could not close" message on an installation that never enabled it would be false.
    /// </summary>
    private async Task<PeriodAutoCloseDecision?> TryResolveAsync(Candidate candidate, CancellationToken cancellationToken)
    {
        try
        {
            var decision = await _autonomyResolver.ResolveAsync(candidate.Group.Id, cancellationToken);
            if (!decision.CanClose)
            {
                _logger.LogDebug(
                    "PeriodAutoClose: automatic closing is not allowed for group {GroupName} (blocked by {BlockedBy})",
                    candidate.Group.Name, decision.BlockedBy);
            }

            return decision;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PeriodAutoClose: resolving the autonomy gate for group {GroupName} failed; the group is skipped",
                candidate.Group.Name);

            return null;
        }
    }

    private async Task<IAgentTriggerEvent?> EvaluateArmedGroupAsync(
        Candidate candidate, DateOnly today, int? lagDays, CancellationToken cancellationToken)
    {
        try
        {
            return await EvaluateCoreAsync(candidate, today, lagDays, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "PeriodAutoClose: closing period {From}..{Until} of group {GroupName} failed; the period stays open",
                candidate.PeriodStart, candidate.PeriodEnd, candidate.Group.Name);

            return Blocked(candidate, PeriodAutoCloseBlockReason.Failed);
        }
    }

    private async Task<IAgentTriggerEvent?> EvaluateCoreAsync(
        Candidate candidate, DateOnly today, int? lagDays, CancellationToken cancellationToken)
    {
        var (group, periodStart, periodEnd) = candidate;

        if (await IsLastDaySealedAsync(_sealedDayRepository, candidate, cancellationToken))
        {
            return null;
        }

        if (!await _activityProbe.HasDirectWorkInRangeAsync(group, periodStart, periodEnd, cancellationToken))
        {
            return null;
        }

        if (await HasReopenHistoryAsync(_auditLogRepository, candidate, cancellationToken))
        {
            LogReopenHistory(candidate);
            return null;
        }

        if (lagDays is not { } lag)
        {
            _logger.LogInformation(
                "PeriodAutoClose: no close lag is stored; period {From}..{Until} of group {GroupName} is not closed automatically",
                periodStart, periodEnd, group.Name);

            return Blocked(candidate, PeriodAutoCloseBlockReason.NoLagStored);
        }

        if (!PeriodCloseDateCalculator.IsAutoCloseDue(today, periodEnd, lag))
        {
            return null;
        }

        if (!PeriodCloseDateCalculator.IsWithinAutoCloseWindow(today, periodEnd, lag, PeriodAutoClose.WindowDays))
        {
            return Blocked(candidate, PeriodAutoCloseBlockReason.CloseWindowMissed);
        }

        if (await HasAnySealedDayAsync(_sealedDayRepository, candidate, cancellationToken))
        {
            return Blocked(candidate, PeriodAutoCloseBlockReason.PartiallySealed);
        }

        return await CloseInFreshScopeAsync(candidate, lag, cancellationToken);
    }

    private async Task<IAgentTriggerEvent?> CloseInFreshScopeAsync(
        Candidate candidate, int lag, CancellationToken cancellationToken)
    {
        var (group, periodStart, periodEnd) = candidate;
        using var scope = _scopeFactory.CreateScope();

        var decision = await scope.ServiceProvider.GetRequiredService<IPeriodAutoCloseResolver>()
            .ResolveAsync(group.Id, cancellationToken);
        if (!decision.CanClose || decision.DecidingAdminUserId is not { } decidingAdmin || decidingAdmin == Guid.Empty)
        {
            _logger.LogWarning(
                "PeriodAutoClose: the autonomy gate closed right before sealing period {From}..{Until} of group {GroupName} (blocked by {BlockedBy}); not closed",
                periodStart, periodEnd, group.Name, decision.BlockedBy);

            return Blocked(candidate, PeriodAutoCloseBlockReason.AutonomyLowered);
        }

        var sealedDays = scope.ServiceProvider.GetRequiredService<ISealedDayRepository>();
        if (await IsLastDaySealedAsync(sealedDays, candidate, cancellationToken))
        {
            return null;
        }

        if (await HasAnySealedDayAsync(sealedDays, candidate, cancellationToken))
        {
            return Blocked(candidate, PeriodAutoCloseBlockReason.PartiallySealed);
        }

        if (await HasReopenHistoryAsync(
                scope.ServiceProvider.GetRequiredService<IPeriodAuditLogRepository>(), candidate, cancellationToken))
        {
            LogReopenHistory(candidate);
            return null;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var issues = await mediator.Send(new GetPeriodIssuesQuery(periodStart, periodEnd, group.Id), cancellationToken);
        var errorCount = issues.Count(issue => issue.Severity == ScheduleValidationType.Error);
        if (errorCount > NoErrors)
        {
            _logger.LogInformation(
                "PeriodAutoClose: period {From}..{Until} of group {GroupName} still holds {Errors} error(s); not closed",
                periodStart, periodEnd, group.Name, errorCount);

            return Blocked(candidate, PeriodAutoCloseBlockReason.OpenErrors, errorCount);
        }

        var reason = string.Format(CultureInfo.InvariantCulture, PeriodAutoClose.ReasonFormat, lag);
        try
        {
            await mediator.Send(
                new ClosePeriodByGroupCommand(periodStart, periodEnd, group.Id, reason, ActingAdminUserId: decidingAdmin),
                cancellationToken);
        }
        catch (PeriodValidationConflictException ex)
        {
            _logger.LogInformation(
                "PeriodAutoClose: the close handler refused period {From}..{Until} of group {GroupName} with {Errors} error(s)",
                periodStart, periodEnd, group.Name, ex.CurrentErrorCount);

            return Blocked(candidate, PeriodAutoCloseBlockReason.OpenErrors, ex.CurrentErrorCount);
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogWarning(
                "PeriodAutoClose: the close handler refused period {From}..{Until} of group {GroupName}: {Message}",
                periodStart, periodEnd, group.Name, ex.Message);

            return Blocked(candidate, PeriodAutoCloseBlockReason.Refused);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return await ResolveFailedCloseAsync(candidate, lag, decidingAdmin, ex, cancellationToken);
        }

        if (!await IsWholePeriodSealedAsync(sealedDays, candidate, cancellationToken))
        {
            _logger.LogError(
                "PeriodAutoClose: period {From}..{Until} of group {GroupName} was sent to the close handler but the read-back does not show it sealed",
                periodStart, periodEnd, group.Name);

            return Blocked(candidate, PeriodAutoCloseBlockReason.NotVerified);
        }

        _logger.LogInformation(
            "PeriodAutoClose: period {From}..{Until} of group {GroupName} closed on behalf of admin {AdminUserId} (close lag {Lag} day(s))",
            periodStart, periodEnd, group.Name, decidingAdmin, lag);

        return new PeriodAutoClosedTriggerEvent(group.Id, group.Name, periodStart, periodEnd, lag, decidingAdmin);
    }

    /// <summary>
    /// A close that threw something other than the handler's own refusals. The typical cause is a person
    /// closing the same period at the same moment: the second seal trips the unique index on the day locks and
    /// its transaction rolls back. The state is therefore read again in ANOTHER fresh scope (the context of the
    /// failed one is not trusted) instead of reporting a raw failure: when the whole period is sealed now and
    /// the audit shows the seal of Klacksy itself, the close did land (report it); when it is sealed by somebody
    /// else, the period is closed and nothing is reported; otherwise the exception stands and becomes Failed.
    /// </summary>
    private async Task<IAgentTriggerEvent?> ResolveFailedCloseAsync(
        Candidate candidate, int lag, Guid decidingAdmin, Exception failure, CancellationToken cancellationToken)
    {
        var (group, periodStart, periodEnd) = candidate;
        using var scope = _scopeFactory.CreateScope();
        var sealedDays = scope.ServiceProvider.GetRequiredService<ISealedDayRepository>();
        if (!await IsLastDaySealedAsync(sealedDays, candidate, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Closing period {periodStart:yyyy-MM-dd}..{periodEnd:yyyy-MM-dd} of group {group.Id} failed and the period is not sealed.",
                failure);
        }

        var auditLog = scope.ServiceProvider.GetRequiredService<IPeriodAuditLogRepository>();
        var entries = await auditLog.GetRangeAsync(periodStart, periodEnd, cancellationToken);
        var decidingAdminId = decidingAdmin.ToString();
        var sealedByKlacksy = entries.Any(entry =>
            entry.Action == PeriodAuditAction.Seal
            && entry.GroupId == group.Id
            && entry.PerformedBy == decidingAdminId
            && entry.PerformedByName == AuditActorDefaults.AutonomousActorName);

        if (sealedByKlacksy && await IsWholePeriodSealedAsync(sealedDays, candidate, cancellationToken))
        {
            _logger.LogWarning(failure,
                "PeriodAutoClose: closing period {From}..{Until} of group {GroupName} reported an error, but the seal is in place",
                periodStart, periodEnd, group.Name);

            return new PeriodAutoClosedTriggerEvent(group.Id, group.Name, periodStart, periodEnd, lag, decidingAdmin);
        }

        _logger.LogInformation(failure,
            "PeriodAutoClose: period {From}..{Until} of group {GroupName} was closed by somebody else at the same moment; nothing to report",
            periodStart, periodEnd, group.Name);

        return null;
    }

    /// <summary>
    /// Whether anybody ever REOPENED the period - an unseal entry of this group or installation-wide overlapping
    /// it. The automatic path only performs the first close of a period: a reopened period was reopened on
    /// purpose, typically to correct it, and sealing it again on the next scan would close it mid-edit and fire
    /// the payroll export a second time. This is the one non-close that stays silent - the person decided, and
    /// the overdue reminder remains. A seal entry alone does not count: a partial seal still holds its day locks
    /// and is reported as PartiallySealed.
    /// </summary>
    private static async Task<bool> HasReopenHistoryAsync(
        IPeriodAuditLogRepository repository, Candidate candidate, CancellationToken cancellationToken)
    {
        var entries = await repository.GetRangeAsync(candidate.PeriodStart, candidate.PeriodEnd, cancellationToken);
        return entries.Any(entry =>
            entry.Action == PeriodAuditAction.Unseal
            && (entry.GroupId == null || entry.GroupId == candidate.Group.Id));
    }

    private void LogReopenHistory(Candidate candidate)
    {
        _logger.LogInformation(
            "PeriodAutoClose: period {From}..{Until} of group {GroupName} was reopened before; only a person closes it again",
            candidate.PeriodStart, candidate.PeriodEnd, candidate.Group.Name);
    }

    private static async Task<bool> IsLastDaySealedAsync(
        ISealedDayRepository repository, Candidate candidate, CancellationToken cancellationToken)
    {
        var sealedDays = await repository.GetRangeAsync(
            candidate.PeriodEnd, candidate.PeriodEnd, candidate.Group.Id, cancellationToken);
        return sealedDays.Count > 0;
    }

    private static async Task<bool> HasAnySealedDayAsync(
        ISealedDayRepository repository, Candidate candidate, CancellationToken cancellationToken)
    {
        var sealedDays = await repository.GetRangeAsync(
            candidate.PeriodStart, candidate.PeriodEnd, candidate.Group.Id, cancellationToken);
        return sealedDays.Count > 0;
    }

    private static async Task<bool> IsWholePeriodSealedAsync(
        ISealedDayRepository repository, Candidate candidate, CancellationToken cancellationToken)
    {
        var sealedDays = await repository.GetRangeAsync(
            candidate.PeriodStart, candidate.PeriodEnd, candidate.Group.Id, cancellationToken);
        var sealedDates = sealedDays.Select(day => day.Date).ToHashSet();
        for (var day = candidate.PeriodStart; day <= candidate.PeriodEnd; day = day.AddDays(1))
        {
            if (!sealedDates.Contains(day))
            {
                return false;
            }
        }

        return true;
    }

    private static PeriodAutoCloseBlockedTriggerEvent Blocked(
        Candidate candidate, PeriodAutoCloseBlockReason reason, int errorCount = NoErrors) =>
        new(candidate.Group.Id, candidate.Group.Name, candidate.PeriodStart, candidate.PeriodEnd, errorCount, reason);

    private sealed record Candidate(Group Group, DateOnly PeriodStart, DateOnly PeriodEnd);
}
