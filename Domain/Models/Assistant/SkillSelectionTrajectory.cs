// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillSelectionTrajectory : BaseEntity
{
    public Guid AgentId { get; set; }

    public Guid? TurnId { get; set; }

    public string? UserId { get; set; }

    public string Locale { get; set; } = string.Empty;

    public string UserMessageHash { get; set; } = string.Empty;

    public string IntentExcerpt { get; set; } = string.Empty;

    public string KnowledgeIndexCandidatesJson { get; set; } = "[]";

    public string? LlmChosenSkill { get; set; }

    public bool WasExecuted { get; set; }

    /// <summary>
    /// Whether every skill execution of this turn succeeded (W1.3). Computed at capture time from the
    /// turn_id join to skill_usage_records: true when the turn ran at least one skill and none failed,
    /// false when at least one failed, null when the turn executed nothing or predates the turn_id join.
    /// </summary>
    public bool? WasSuccessful { get; set; }

    public bool HadMutationIntent { get; set; }

    public bool WasCorrected { get; set; }

    public string CorrectionType { get; set; } = CorrectionTypes.None;

    public int LatencyMsTotal { get; set; }

    public int LatencyMsKnowledge { get; set; }

    public int LatencyMsLlm { get; set; }

    public Guid? PlanId { get; set; }

    /// <summary>
    /// When the description optimizer consumed this correction as evidence for a proposal. Set once and
    /// never cleared: it is the watermark that stops the same correction from producing a second
    /// sharpening proposal on every later run.
    /// </summary>
    public DateTime? SharpenedAtUtc { get; set; }

    /// <summary>
    /// Name of the recipe that was forcing this turn, null when no recipe was active. The only link
    /// between a turn and a composed capability, and therefore the denominator of that capability's
    /// usefulness quote.
    /// </summary>
    public string? RecipeName { get; set; }

    /// <summary>
    /// Owner of a learned phrase whose wording occurs in this turn's excerpt, null when none does.
    /// Recorded at capture time rather than derived later, so a phrase learned tomorrow cannot claim
    /// credit for a turn that happened yesterday. This is a substring heuristic, not causality: the
    /// phrase may have occurred without having contributed anything to the routing.
    /// </summary>
    public string? LearnedPhraseHit { get; set; }

    /// <summary>
    /// True when the user gave the answer a thumbs-up, null while they said nothing. Deliberately
    /// nullable: "nobody judged this turn" and "somebody judged it unhelpful" are different facts, and
    /// the fitness quote may only count the first as neutral.
    /// </summary>
    public bool? Helpful { get; set; }

    /// <summary>
    /// Optional free-text the user attached to a not-helpful judgement (W1.8). Null for thumbs-up and
    /// for unjudged turns. Truncated to <see cref="SkillLearningDefaults.FeedbackCommentMaxLength"/> by
    /// the feedback handler, never rejected.
    /// </summary>
    public string? HelpfulComment { get; set; }
}
