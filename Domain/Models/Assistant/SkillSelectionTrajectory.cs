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

    public bool WasCorrected { get; set; }

    public string CorrectionType { get; set; } = CorrectionTypes.None;

    public int LatencyMsTotal { get; set; }

    public int LatencyMsKnowledge { get; set; }

    public int LatencyMsLlm { get; set; }

    public Guid? PlanId { get; set; }
}
