// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stable identifiers for the entity a persisted company rule was applied against, stored in
/// <see cref="Klacks.Api.Domain.Models.Settings.CompanyRule.TargetEntityType"/>. Settings has no single
/// row id, so a surcharge rule targets the settings collection as a whole (no TargetEntityId).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class CompanyRuleTargetEntityTypes
{
    public const string Settings = "settings";
    public const string CounterRule = "counterRule";
    public const string Macro = "macro";
}
