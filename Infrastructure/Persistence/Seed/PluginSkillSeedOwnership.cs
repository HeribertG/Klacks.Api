// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which skill definitions of a feature plugin's skill-seeds.json the plugin may write. A plugin
/// owns a skill only when its manifest lists the name in providedSkills AND the name is not a core skill
/// from the core skill-seeds.json; every other definition is skipped with a warning. Without this rule a
/// plugin seed could carry the name of a core skill and, with a higher version, overwrite its required
/// permission, handler type or handler config. Name comparison is case-insensitive, matching how the seed
/// loader looks up stored skills. Fails closed: an unreadable manifest yields no provided skills, and an
/// unreadable core seed file lets no plugin definition through.
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Models.Plugins;
using Klacks.Api.Infrastructure.Persistence.Seed.Models;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public static class PluginSkillSeedOwnership
{
    private const string CoreSkillsProperty = "skills";
    private const string SkillNameProperty = "name";

    private static readonly JsonSerializerOptions ManifestReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<SkillSeedDefinition> SelectOwned(
        string pluginName,
        IEnumerable<SkillSeedDefinition> definitions,
        IReadOnlyCollection<string> providedSkills,
        IReadOnlySet<string> coreSkillNames,
        ILogger logger)
    {
        var provided = new HashSet<string>(providedSkills, StringComparer.OrdinalIgnoreCase);
        var owned = new List<SkillSeedDefinition>();

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                continue;
            }

            if (coreSkillNames.Contains(definition.Name))
            {
                logger.LogWarning(
                    "Plugin '{Plugin}' skill seed '{Skill}' skipped: the name belongs to a core skill",
                    pluginName,
                    definition.Name);
                continue;
            }

            if (!provided.Contains(definition.Name))
            {
                logger.LogWarning(
                    "Plugin '{Plugin}' skill seed '{Skill}' skipped: not listed in the manifest's providedSkills",
                    pluginName,
                    definition.Name);
                continue;
            }

            owned.Add(definition);
        }

        return owned;
    }

    public static async Task<IReadOnlyCollection<string>> ReadProvidedSkillsAsync(
        string manifestPath, ILogger logger, CancellationToken cancellationToken)
    {
        if (!File.Exists(manifestPath))
        {
            logger.LogWarning("Plugin manifest {ManifestPath} not found; no plugin skill seeds are applied", manifestPath);
            return [];
        }

        try
        {
            await using var stream = File.OpenRead(manifestPath);
            var manifest = await JsonSerializer.DeserializeAsync<FeaturePluginManifest>(
                stream, ManifestReadOptions, cancellationToken);
            return manifest?.ProvidedSkills ?? [];
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Plugin manifest {ManifestPath} is unreadable; no plugin skill seeds are applied", manifestPath);
            return [];
        }
    }

    public static async Task<IReadOnlySet<string>?> ReadCoreSkillNamesAsync(
        string coreSeedPath, ILogger logger, CancellationToken cancellationToken)
    {
        if (!File.Exists(coreSeedPath))
        {
            logger.LogError("Core skill seed file {SeedPath} not found; plugin skill seeds are not applied", coreSeedPath);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(coreSeedPath);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (TryGetPropertyIgnoreCase(document.RootElement, CoreSkillsProperty, out var skills)
                && skills.ValueKind == JsonValueKind.Array)
            {
                foreach (var skill in skills.EnumerateArray())
                {
                    if (skill.ValueKind == JsonValueKind.Object
                        && TryGetPropertyIgnoreCase(skill, SkillNameProperty, out var name)
                        && name.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(name.GetString()))
                    {
                        names.Add(name.GetString()!);
                    }
                }
            }

            return names;
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Core skill seed file {SeedPath} is unreadable; plugin skill seeds are not applied", coreSeedPath);
            return null;
        }
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement element, string propertyName, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }
}
