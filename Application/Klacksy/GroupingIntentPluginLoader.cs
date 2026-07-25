// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads grouping-intent.json from each installed language plugin directory and passes the
/// grouping and location/assignment keywords to GroupingIntentResolver.Configure().
/// Called once at application startup; plugin languages extend the core de/en/fr/it detection.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Klacksy;

public static class GroupingIntentPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void Load(string baseDirectory, Action<string, Exception>? onError = null)
    {
        var pluginDir = Path.Combine(baseDirectory, LanguagePluginConstants.PluginDirectory);
        if (!Directory.Exists(pluginDir))
            return;

        var allGroupingTokens = new List<string>();
        var allLocationOrAssignmentTokens = new List<string>();

        foreach (var langDir in Directory.GetDirectories(pluginDir))
        {
            var code = Path.GetFileName(langDir);
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
                continue;

            var file = Path.Combine(langDir, LanguagePluginConstants.GroupingIntentFileName);
            if (!File.Exists(file))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<GroupingIntentData>(json, JsonOptions);
                if (data == null) continue;

                allGroupingTokens.AddRange(data.GroupingTokens);
                allLocationOrAssignmentTokens.AddRange(data.LocationOrAssignmentTokens);
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, ex);
            }
        }

        if (allGroupingTokens.Count > 0 || allLocationOrAssignmentTokens.Count > 0)
            GroupingIntentResolver.Configure(allGroupingTokens, allLocationOrAssignmentTokens);
    }

    private sealed class GroupingIntentData
    {
        [JsonPropertyName("groupingTokens")]
        public string[] GroupingTokens { get; set; } = [];

        [JsonPropertyName("locationOrAssignmentTokens")]
        public string[] LocationOrAssignmentTokens { get; set; } = [];
    }
}
