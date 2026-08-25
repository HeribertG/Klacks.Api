// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One registry row: which composite Act-skill remediates a trigger kind, and how to turn that kind's
/// condition payload into that skill's arguments. Existence of an entry is itself a permission - see
/// IConditionRemediationRegistry.TryGetEffectiveMaxAction - so this type carries no MaxAction of its
/// own; how far Klacksy may go still comes from agent_trigger_governance (Etappe 4a).
/// </summary>
/// <param name="RemediationSkillName">The Act-skill name that carries the remediation out (e.g. a future scenario-capable composite skill).</param>
/// <param name="ParameterBinder">Deterministic payload-to-arguments translation for that skill.</param>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ConditionRemediationEntry(
    string RemediationSkillName,
    IConditionRemediationParameterBinder ParameterBinder);
