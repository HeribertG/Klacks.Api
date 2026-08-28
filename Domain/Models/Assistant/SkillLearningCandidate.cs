// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One generated variant of an artefact that could close a learning cluster, together with the verdicts
/// of the routing and execution oracles. Nothing writes rows of this type before stage G2; the table
/// exists in G1 so the loop can be added without a second schema change.
/// </summary>
/// <param name="Kind">phrase, capability or description, see SkillLearningCandidateKinds</param>
/// <param name="PayloadJson">The proposed artefact itself, shape depending on Kind</param>
/// <param name="RoutingResultJson">Verdict of the routing oracle O1</param>
/// <param name="ExecutionResultJson">Verdict of the execution oracle O2, capability candidates only</param>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillLearningCandidate : BaseEntity
{
    public Guid ClusterId { get; set; }

    public int VariantNo { get; set; }

    public string Kind { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = "{}";

    public string Status { get; set; } = SkillLearningCandidateStatuses.Generated;

    public string? RoutingResultJson { get; set; }

    public string? ExecutionResultJson { get; set; }

    public string? ErrorText { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }
}
