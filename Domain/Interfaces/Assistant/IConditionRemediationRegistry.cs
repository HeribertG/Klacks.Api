// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deterministic, code-defined map from a proactive trigger kind to the one composite Act-skill that
/// remediates it (Etappe 4b of the Klacksy-proactive plan). This is the SECOND, independent security
/// gate for the action branch: agent_trigger_governance (Etappe 4a) says how far a kind is ALLOWED to
/// go, but a kind absent from this registry has nothing that could carry a remediation out - no scenario
/// exists to prepare, no skill exists to execute - so it can never be steered past Hint regardless of
/// what governance or a delegation (Etappe 4e) configured. Only a code change - reviewed, deployed, not
/// runtime-editable - can add an entry, which is deliberate: unlike governance, this gate is not meant
/// to be operable by an admin.
/// </summary>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IConditionRemediationRegistry
{
    /// <summary>
    /// Every trigger kind that has a remediation. The action dispatcher iterates this rather than the
    /// governed-kind list, so a kind nothing can remediate never costs a governance lookup or a ledger
    /// query per tick.
    /// </summary>
    IReadOnlyCollection<string> RegisteredKinds { get; }

    /// <summary>The registered remediation for this trigger kind, if any.</summary>
    bool TryGetEntry(string triggerKind, out ConditionRemediationEntry? entry);

    /// <summary>
    /// Caps <paramref name="configuredMaxAction"/> at Hint unless <paramref name="triggerKind"/> has a
    /// registry entry. Callers pass the governance-resolved value here (Etappe 4a's
    /// ProactiveGovernanceDecision.EffectiveMaxAction, itself already folding in the kill switch and the
    /// Enabled flag) - this method folds in the registry on top, so the caller never has to remember to
    /// consult both gates separately. Hint always passes through unchanged: a kind that only reports and
    /// waits needs no remediation to do that.
    /// </summary>
    ProactiveMaxAction TryGetEffectiveMaxAction(string triggerKind, ProactiveMaxAction configuredMaxAction);
}
