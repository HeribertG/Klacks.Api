// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic, LLM-free translation from a condition-ledger row's free-form payload into the typed
/// arguments a remediation skill expects. Implementations are pure functions: same payload in, same
/// skill arguments out, no I/O, no randomness, no model call - the plan's "kein LLM im Heartbeat-Pfad"
/// rule applies to remediation parameter binding just as much as to the detectors themselves.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConditionRemediationParameterBinder
{
    /// <summary>
    /// Builds the skill argument set for one condition. <paramref name="conditionPayload"/> is the
    /// deserialized <c>AgentCondition.PayloadJson</c> the detector captured; the result is handed to
    /// <c>ISkillExecutor</c> as-is.
    /// </summary>
    IReadOnlyDictionary<string, object?> Bind(IReadOnlyDictionary<string, object?> conditionPayload);
}
