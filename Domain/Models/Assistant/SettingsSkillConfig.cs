// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration for a settings reader skill defining which settings keys to read and how to present them.
/// </summary>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record SettingsSkillConfig
{
    public required string SkillName { get; init; }
    public required string SkillDescription { get; init; }
    public required SkillCategory SkillCategory { get; init; }
    public required IReadOnlyList<string> Permissions { get; init; }
    public required string ResultMessage { get; init; }
    public required IReadOnlyList<SettingsField> Fields { get; init; }
    public IReadOnlyList<SensitiveSettingsField>? SensitiveFields { get; init; }
}

public record SettingsField(string SettingsKey, string ResultName);

public record SensitiveSettingsField(string SettingsKey, string ResultName);
