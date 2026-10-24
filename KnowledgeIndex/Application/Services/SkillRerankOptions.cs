// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime switch for the cross-encoder on skill passes, bound from KnowledgeIndex:SkillRerankMode and
/// KnowledgeIndex:SkillRerankMarginThreshold. Background (eval 2026-10-24): the skill rerank costs ~1.6 s per turn,
/// three times the model call, and a diagnostic of 2026-07-31 (KnowledgeIndexPhraseFacetDiagnosticsTests) measured
/// the cross-encoder BELOW plain vector order on tool descriptions (84.1 % against 92.8 %). The default stays Always
/// until a turn eval with Adaptive or Never confirms that on today's catalogue.
/// Note for Adaptive/Never: the kept scores are cosine similarities, which sit much closer together than
/// cross-encoder scores. Consumers that compare score differences (the correction clarification in
/// CorrectionOutcomeComposer) therefore read more pairs as ambiguous; measure the correction goldset as well.
/// </summary>
/// <param name="Mode">Always, Adaptive or Never</param>
/// <param name="MarginThreshold">Adaptive only: the cosine gap between the first two vector candidates below
/// which the pass is still reranked</param>

using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public sealed record SkillRerankOptions(SkillRerankMode Mode, double MarginThreshold)
{
    public static SkillRerankOptions Default { get; } =
        new(SkillRerankMode.Always, KnowledgeIndexConstants.DefaultSkillRerankMarginThreshold);

    /// <summary>
    /// An unknown or empty mode falls back to Always, so a typo in the configuration never silently switches the
    /// cross-encoder off.
    /// </summary>
    /// <param name="mode">The configured mode name, case-insensitive</param>
    /// <param name="marginThreshold">The configured margin threshold</param>
    public static SkillRerankOptions FromConfiguration(string? mode, double marginThreshold) =>
        new(
            Enum.TryParse<SkillRerankMode>(mode, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                ? parsed
                : SkillRerankMode.Always,
            marginThreshold);
}
