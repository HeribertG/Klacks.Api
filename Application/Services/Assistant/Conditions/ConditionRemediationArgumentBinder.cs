// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a condition's payload into the remediation skill's arguments, twice per action: once BEFORE the
/// claim, so an unbindable row costs neither an attempt nor a slot of the daily action budget, and once
/// AFTER it against the row as it stands then. The second pass exists because PayloadJson is no longer
/// write-once (2026-08-26): any detector tick can rewrite it while the row sits between the tick's
/// snapshot and the claim, and executing the pre-flight arguments could write a container template from
/// a definition a human has since corrected. Extracted from AgentConditionActionService so that class
/// stays under its size-guard ceiling; the semantics are unchanged.
/// </summary>
/// <param name="repository">Re-reads the claimed row (AsNoTracking) for the post-claim pass.</param>
/// <param name="logger">Names the missing arguments and the payload changes; neither is an error.</param>

using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionRemediationArgumentBinder
{
    private const string PayloadChangedDuringClaimMessage =
        "Condition {ConditionId} had its payload refreshed between the pre-flight binding and the claim; "
        + "re-binding {Skill} against the current one";

    private readonly IAgentConditionRepository _repository;
    private readonly ILogger _logger;

    public ConditionRemediationArgumentBinder(IAgentConditionRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <summary>
    /// The remediation's arguments, or null when this condition cannot produce them. Null is NOT a
    /// failure to be retried. Since 2026-08-26 a row CAN become bindable while it stays open - a
    /// re-observation refreshes PayloadJson, so a binder that gains a required field reaches the existing
    /// backlog on the next tick that still reports it. What stays permanently unbindable is a row whose
    /// underlying entity simply does not carry what the binder needs (see EmptyContainerRemediationBinder's
    /// weekday and end-after-start cases); those are skipped quietly on every tick, by design.
    /// </summary>
    public IReadOnlyDictionary<string, object?>? TryBind(ConditionRemediationEntry entry, AgentCondition condition)
    {
        Dictionary<string, object?>? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Dictionary<string, object?>>(condition.PayloadJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex, "Condition {ConditionId} carries a payload that is not valid JSON and cannot be remediated",
                condition.Id);
            return null;
        }

        if (payload is null)
        {
            return null;
        }

        var arguments = entry.ParameterBinder.Bind(payload);
        var missing = entry.RequiredArguments
            .Where(name => !arguments.TryGetValue(name, out var value) || value is null)
            .ToList();

        if (missing.Count == 0)
        {
            return arguments;
        }

        _logger.LogInformation(
            "Condition {ConditionId} cannot be remediated by {Skill}: the payload yields no {Missing}. "
            + "It stays reported and visible to planners, and costs neither an attempt nor action budget",
            condition.Id, entry.RemediationSkillName, string.Join(", ", missing));

        return null;
    }

    /// <summary>
    /// The arguments to actually execute with, re-derived from the row as it stands AFTER the claim, or
    /// null when it no longer binds. GetByIdAsync reads AsNoTracking, so this sees what the database holds
    /// now rather than the snapshot instance the change tracker would hand back. The payload is compared
    /// first because it is unchanged in almost every claim. A row that stopped binding is deliberately
    /// left claimed rather than pushed to a terminal status - the stale-claim reclaim exists for exactly
    /// this, and the next tick binds it from the payload that made it change.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, object?>?> RebindAfterClaimAsync(
        ConditionRemediationEntry entry,
        AgentCondition condition,
        IReadOnlyDictionary<string, object?> preflightArguments,
        CancellationToken cancellationToken)
    {
        var claimed = await _repository.GetByIdAsync(condition.Id, cancellationToken);
        if (claimed is null)
        {
            return null;
        }

        if (string.Equals(claimed.PayloadJson, condition.PayloadJson, StringComparison.Ordinal))
        {
            return preflightArguments;
        }

        _logger.LogInformation(PayloadChangedDuringClaimMessage, condition.Id, entry.RemediationSkillName);

        return TryBind(entry, claimed);
    }
}
