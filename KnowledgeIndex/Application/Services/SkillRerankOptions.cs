// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runtime switch for the cross-encoder on skill passes, bound from KnowledgeIndex:SkillRerankMode and
/// KnowledgeIndex:SkillRerankMarginThreshold. Background (eval 2026-10-24): the skill rerank costs ~1.6 s per turn,
/// three times the model call, and a diagnostic of 2026-07-31 (KnowledgeIndexPhraseFacetDiagnosticsTests) measured
/// the cross-encoder BELOW plain vector order on tool descriptions (84.1 % against 92.8 %). Since 2026-11-13 an
/// installation without the key runs Never: the assembler goldsets (IT 595ffb7) measured the same recall in both
/// modes and 2.2 s less per turn without the cross-encoder.
/// Note for Adaptive/Never: the kept scores are cosine similarities, which sit much closer together than
/// cross-encoder scores. The correction clarification in CorrectionOutcomeComposer compares score differences and
/// asked back in 11 of 14 correction-v1 cases on cosines, so correction turns force the rerank
/// (IKnowledgeRetrievalService.RetrieveAsync, forceSkillRerank).
/// </summary>
/// <param name="Mode">Always, Adaptive or Never</param>
/// <param name="MarginThreshold">Adaptive only: the cosine gap between the first two vector candidates below
/// which the pass is still reranked</param>

using Klacks.Api.KnowledgeIndex.Application.Constants;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public sealed record SkillRerankOptions(SkillRerankMode Mode, double MarginThreshold)
{
    /// <summary>The mode of an installation whose configuration does not set KnowledgeIndex:SkillRerankMode.</summary>
    public const SkillRerankMode ShippedMode = SkillRerankMode.Never;

    /// <summary>
    /// The cross-encoder on every skill pass. Used by hand-built services (tests, tools) that pass no options; an
    /// installation binds its options through FromConfiguration instead.
    /// </summary>
    public static SkillRerankOptions AlwaysRerank { get; } =
        new(SkillRerankMode.Always, KnowledgeIndexConstants.DefaultSkillRerankMarginThreshold);

    /// <summary>
    /// Adaptive only: the share of passes whose decision is flipped at random for the threshold tuner. 0 unless
    /// configured, so a hand-built options object (tests, tools) is deterministic; the configuration supplies
    /// DefaultSkillRerankExplorationRate.
    /// </summary>
    public double ExplorationRate { get; init; }

    /// <summary>
    /// A missing or blank mode is the shipped default, ShippedMode (Never). An unknown value falls back to
    /// Always, so a typo in the configuration never silently switches the cross-encoder off.
    /// </summary>
    /// <param name="mode">The configured mode name, case-insensitive</param>
    /// <param name="marginThreshold">The configured starting margin threshold</param>
    /// <param name="explorationRate">The configured exploration rate</param>
    public static SkillRerankOptions FromConfiguration(
        string? mode, double marginThreshold, double explorationRate = KnowledgeIndexConstants.DefaultSkillRerankExplorationRate) =>
        new(
            string.IsNullOrWhiteSpace(mode)
                ? ShippedMode
                : Enum.TryParse<SkillRerankMode>(mode, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : SkillRerankMode.Always,
            marginThreshold)
        {
            ExplorationRate = explorationRate
        };
}
