// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class ProactiveGovernanceRuleDto
{
    public string TriggerKind { get; set; } = string.Empty;

    public Guid? GroupId { get; set; }

    public int MaxAction { get; set; }

    public string MaxActionName { get; set; } = string.Empty;

    public int EffectiveMaxAction { get; set; }

    /// <summary>The installation-wide ceiling the global autonomy level imposes on this rule.</summary>
    public int GlobalAutonomyCap { get; set; }

    /// <summary>
    /// False when this kind has no remediation that could be laid in front of a human as a scenario, so
    /// the Prepare rung can never take effect for it and the settings card must not offer it. Comes from
    /// the code-only remediation registry, not from the stored rule.
    /// </summary>
    public bool IsScenarioCapable { get; set; }

    public bool Enabled { get; set; }

    public int DailyActionBudget { get; set; }

    public int WindowActionLimit { get; set; }

    public int WindowMinutes { get; set; }

    public bool IsStored { get; set; }
}
