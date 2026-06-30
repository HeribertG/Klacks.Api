// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads navigation-intent.json from each installed language plugin directory and passes
/// the question leads and navigation phrases to NavigationIntentDetector.Configure().
/// Called once at application startup; plugin languages extend the core de/en/fr/it detection.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Klacksy;

public static class NavigationIntentPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void Load(string baseDirectory)
    {
        var pluginDir = Path.Combine(baseDirectory, LanguagePluginConstants.PluginDirectory);
        if (!Directory.Exists(pluginDir))
            return;

        var allLeads = new List<string>();
        var allPhrases = new List<string>();

        foreach (var langDir in Directory.GetDirectories(pluginDir))
        {
            var code = Path.GetFileName(langDir);
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
                continue;

            var file = Path.Combine(langDir, "navigation-intent.json");
            if (!File.Exists(file))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<NavigationIntentData>(json, JsonOptions);
                if (data == null) continue;

                allLeads.AddRange(data.InfoQuestionLeads);
                allPhrases.AddRange(data.NavigationPhrases);
            }
            catch
            {
            }
        }

        if (allLeads.Count > 0 || allPhrases.Count > 0)
            NavigationIntentDetector.Configure(allLeads, allPhrases);
    }

    private sealed class NavigationIntentData
    {
        [JsonPropertyName("infoQuestionLeads")]
        public string[] InfoQuestionLeads { get; set; } = [];

        [JsonPropertyName("navigationPhrases")]
        public string[] NavigationPhrases { get; set; } = [];
    }
}
