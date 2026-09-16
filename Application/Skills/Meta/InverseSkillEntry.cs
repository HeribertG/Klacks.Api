// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One entry of InverseSkillRegistry. The first two members are the original, prose half, read by
/// SkillRiskClassifier (is this skill reversible at all?) and by RollbackMyLastChangeSkill (which skill
/// would a human call?). The last three are the structured half added 2026-09-16 for the correction undo
/// offer: CopiedArgumentNames are argument names that exist on BOTH skills with the same meaning and are
/// taken verbatim from the call being undone, while ResultIdProperty/ResultIdArgument express the
/// create/delete shape, where the id exists only in the result of the original call (PascalCase, as
/// LLMFunctionExecutor serializes it). An entry declares one or the other, never both, and a __manual__
/// entry declares neither.
/// </summary>
/// <param name="SkillName">Inverse skill, or InverseSkillRegistry.ManualMarker when there is none.</param>
/// <param name="ParamHint">Human-readable note for the rollback proposal; unchanged from before.</param>
/// <param name="CopiedArgumentNames">Arguments copied verbatim from the call being undone.</param>
/// <param name="ResultIdProperty">Result property of the original call holding the created id.</param>
/// <param name="ResultIdArgument">Argument of the inverse skill that id is passed as.</param>
/// <param name="UndoOnly">
/// True when the entry exists ONLY so the correction path can build an undo, and must therefore stay
/// invisible to the prose view (InverseSkillRegistry.TryGetRollbackEntry), which is what
/// SkillRiskClassifier and RollbackMyLastChangeSkill read. Reversibility there is not a statement about
/// the data, it is the autonomy gate: a Reversible skill runs unattended where an Irreversible one is
/// held for confirmation. The entries added 2026-09-16 do declare a lossless inverse, but letting that
/// silently release those write skills from the gate is a separate decision and needs the owner's
/// approval (spec §8), so they are marked here and the risk class of every seeded skill stays exactly as
/// SkillRiskReversibilityPinTests pinned it. Lifting the flag for an approved pair is a one-word diff,
/// and the pin test will show the autonomy change it causes.
/// </param>

namespace Klacks.Api.Application.Skills.Meta;

public sealed record InverseSkillEntry(
    string SkillName,
    string ParamHint,
    IReadOnlyList<string>? CopiedArgumentNames = null,
    string? ResultIdProperty = null,
    string? ResultIdArgument = null,
    bool UndoOnly = false)
{
    private static readonly IReadOnlyList<string> NoCopiedArguments = [];

    public IReadOnlyList<string> CopiedArguments => CopiedArgumentNames ?? NoCopiedArguments;
}
