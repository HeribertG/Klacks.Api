// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class ProactiveGovernanceRuleDto
{
    public string TriggerKind { get; set; } = string.Empty;

    public Guid? GroupId { get; set; }

    public int MaxAction { get; set; }

    public string MaxActionName { get; set; } = string.Empty;

    public int EffectiveMaxAction { get; set; }

    public bool Enabled { get; set; }

    public Guid? ResponsibleOwnerUserId { get; set; }

    public int DailyActionBudget { get; set; }

    public int WindowActionLimit { get; set; }

    public int WindowMinutes { get; set; }

    public bool IsStored { get; set; }
}
