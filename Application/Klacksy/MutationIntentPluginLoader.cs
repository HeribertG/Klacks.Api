// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads mutation-intent.json from each installed language plugin directory and passes
/// the question leads and mutation phrases to MutationIntentDetector.Configure().
/// Called once at application startup; plugin languages extend the core de/en/fr/it detection.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Klacksy;

public static class MutationIntentPluginLoader
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

            var file = Path.Combine(langDir, LanguagePluginConstants.MutationIntentFileName);
            if (!File.Exists(file))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<MutationIntentData>(json, JsonOptions);
                if (data == null) continue;

                allLeads.AddRange(data.InfoQuestionLeads);
                allPhrases.AddRange(data.MutationPhrases);
            }
            catch
            {
            }
        }

        if (allLeads.Count > 0 || allPhrases.Count > 0)
            MutationIntentDetector.Configure(allLeads, allPhrases);
    }

    private sealed class MutationIntentData
    {
        [JsonPropertyName("infoQuestionLeads")]
        public string[] InfoQuestionLeads { get; set; } = [];

        [JsonPropertyName("mutationPhrases")]
        public string[] MutationPhrases { get; set; } = [];
    }
}
