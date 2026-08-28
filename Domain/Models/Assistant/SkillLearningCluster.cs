// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One recurring wish the assistant could not serve, identified by the normalised hash of the utterance.
/// Replaces SkillGapRecord: the cluster is the stable counter across the whole lifecycle, whereas the old
/// record started counting from one again as soon as its status moved on. No message text is stored -
/// only an excerpt of at most 120 characters.
/// </summary>
/// <param name="ClusterKey">MessageNormalizer hash of the utterance, unique per agent</param>
/// <param name="SignalKindsJson">Counter per signal kind, e.g. {"refusal":4,"wrong_skill":1}</param>
/// <param name="OutcomeRef">Id of the created skill_phrase row, or the name of the created agent_recipes row</param>
/// <param name="LearningInstance">Machine name of the instance holding the current claim</param>
using System.ComponentModel.DataAnnotations.Schema;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillLearningCluster : BaseEntity
{
    public Guid AgentId { get; set; }

    public string ClusterKey { get; set; } = string.Empty;

    public string IntentExcerpt { get; set; } = string.Empty;

    public string Locale { get; set; } = string.Empty;

    public int OccurrenceCount { get; set; }

    public int DistinctUserCount { get; set; }

    public string SignalKindsJson { get; set; } = "{}";

    /// <summary>
    /// Centroid of the cluster, null while no embedding was computed. Deliberately NotMapped: pgvector
    /// columns are created and queried through raw SQL everywhere in this project (see KnowledgeEntry
    /// and KnowledgeIndexRepository), because EF Core cannot map the vector type. Nothing populates this
    /// before stage G2.
    /// </summary>
    [NotMapped]
    public float[]? Embedding { get; set; }

    public string Status { get; set; } = SkillLearningClusterStatuses.Collecting;

    public string? OutcomeRefKind { get; set; }

    public string? OutcomeRef { get; set; }

    public DateTime? LearningClaimedAtUtc { get; set; }

    public string? LearningInstance { get; set; }

    public string? LastError { get; set; }

    public int AttemptCount { get; set; }

    /// <summary>
    /// When the cluster last changed Status. Deliberately separate from UpdateTime, which every further
    /// occurrence refreshes: the weekly digest asks "what became reportable last week", and a cluster
    /// that merely recurred must not answer that question again week after week.
    /// </summary>
    public DateTime StatusChangedAtUtc { get; set; }

    public DateTime FirstSeenAtUtc { get; set; }

    public DateTime LastSeenAtUtc { get; set; }

    public DateTime? LearnedAtUtc { get; set; }

    public DateTime? RetiredAtUtc { get; set; }
}
