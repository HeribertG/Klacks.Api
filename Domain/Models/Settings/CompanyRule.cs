// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A registry entry for a company rule the assistant collected and applied: the admin's original
/// wording (RuleText), the rule Kind, which entity it was written to (TargetEntityType/TargetEntityId),
/// a JSON snapshot of the overwritten values for a later revert (SettingsSnapshotJson) and the JSON of
/// the parameters that were applied (AppliedParametersJson).
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings;

public class CompanyRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string RuleText { get; set; } = string.Empty;

    public CompanyRuleKind Kind { get; set; }

    public string TargetEntityType { get; set; } = string.Empty;

    public Guid? TargetEntityId { get; set; }

    public string? SettingsSnapshotJson { get; set; }

    public string AppliedParametersJson { get; set; } = string.Empty;
}
