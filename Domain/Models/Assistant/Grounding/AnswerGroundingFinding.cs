// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One persisted shadow-run finding: a turn whose answer stated hard claims that no grounding
/// source covered. Carries the evidence snapshot because tool results are never persisted
/// anywhere else — without it no later audit could judge the finding.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant.Grounding;

public class AnswerGroundingFinding : BaseEntity
{
    public Guid AgentId { get; set; }

    public string? ConversationId { get; set; }

    public int Tier { get; set; }

    public int EvaluatorVersion { get; set; }

    public string Mode { get; set; } = string.Empty;

    public int ClaimsExtracted { get; set; }

    public int ClaimsUncovered { get; set; }

    public string UncoveredClaimsJson { get; set; } = string.Empty;

    public string ResponseExcerpt { get; set; } = string.Empty;

    public string EvidenceExcerpt { get; set; } = string.Empty;

    public bool EmptyDataDespiteSuccess { get; set; }
}
