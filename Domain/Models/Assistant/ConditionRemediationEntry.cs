// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One registry row: which composite Act-skill remediates a trigger kind, how to turn that kind's
/// condition payload into that skill's arguments, and whether that remediation can be laid in front of
/// a human as an AnalyseScenario first. Existence of an entry is itself a permission - see
/// IConditionRemediationRegistry.TryGetEffectiveMaxAction - so this type carries no MaxAction of its
/// own; how far Klacksy may go still comes from agent_trigger_governance (Etappe 4a).
/// </summary>
/// <param name="RemediationSkillName">The Act-skill name that carries the remediation out.</param>
/// <param name="ParameterBinder">Deterministic payload-to-arguments translation for that skill.</param>
/// <param name="RequiredArguments">
/// The argument names the skill refuses to run without. Declared here, alongside the binder that has to
/// produce them, so the dispatcher can tell "this condition cannot be remediated" from "this remediation
/// failed" BEFORE it claims the row - an unbindable condition must cost neither an attempt nor a slot of
/// the daily action budget. Reading them off the skill descriptor instead would tie the pre-flight to
/// database-seeded metadata that no review of this entry ever looks at.
/// </param>
/// <param name="IsScenarioCapable">
/// False for an Execute-only remediation: a structural Shift change that
/// AcceptAnalyseScenarioCommandHandler could never promote back out of a scenario. The dispatcher must
/// not stage one - it would create an AnalyseScenario nobody can accept, which then ages into a
/// scenario_pending finding of its own. A kind governed at Prepare whose entry is Execute-only
/// therefore reports and waits, exactly as Hint does.
/// </param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConditionRemediationEntry(
    string RemediationSkillName,
    IConditionRemediationParameterBinder ParameterBinder,
    IReadOnlyList<string> RequiredArguments,
    bool IsScenarioCapable = false);
