// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads conversation-signals.json from each installed language plugin directory and passes the
/// affirmation, negation, correction, cancellation and gap-indicator entries to the respective
/// static detectors (AffirmationDetector, ImplicitCorrectionDetector, RecipeCancellationDetector,
/// SkillGapDetector). Called once at application startup; plugin languages extend the core
/// de/en/fr/it detection so confirmations and aborts are understood in every supported language.
/// </summary>
/// <param name="baseDirectory">Application base directory containing the Plugins folder</param>
/// <param name="onError">Optional callback invoked per plugin file that failed to load</param>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Klacksy;

public static class ConversationSignalsPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void Load(string baseDirectory, Action<string, Exception>? onError = null)
    {
        var pluginDir = Path.Combine(baseDirectory, LanguagePluginConstants.PluginDirectory);
        if (!Directory.Exists(pluginDir))
            return;

        var affirmations = new List<string>();
        var negations = new List<string>();
        var corrections = new List<string>();
        var cancellations = new List<string>();
        var gapIndicators = new List<string>();

        foreach (var langDir in Directory.GetDirectories(pluginDir))
        {
            var code = Path.GetFileName(langDir);
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
                continue;

            var file = Path.Combine(langDir, LanguagePluginConstants.ConversationSignalsFileName);
            if (!File.Exists(file))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<ConversationSignalsData>(json, JsonOptions);
                if (data == null) continue;

                affirmations.AddRange(data.Affirmations);
                negations.AddRange(data.Negations);
                corrections.AddRange(data.Corrections);
                cancellations.AddRange(data.Cancellations);
                gapIndicators.AddRange(data.GapIndicators);
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, ex);
            }
        }

        if (affirmations.Count > 0 || negations.Count > 0)
            AffirmationDetector.Configure(affirmations, negations);

        if (corrections.Count > 0)
            ImplicitCorrectionDetector.Configure(corrections);

        if (cancellations.Count > 0)
            RecipeCancellationDetector.Configure(cancellations);

        if (gapIndicators.Count > 0)
            SkillGapDetector.Configure(gapIndicators);
    }

    private sealed class ConversationSignalsData
    {
        [JsonPropertyName("affirmations")]
        public string[] Affirmations { get; set; } = [];

        [JsonPropertyName("negations")]
        public string[] Negations { get; set; } = [];

        [JsonPropertyName("corrections")]
        public string[] Corrections { get; set; } = [];

        [JsonPropertyName("cancellations")]
        public string[] Cancellations { get; set; } = [];

        [JsonPropertyName("gapIndicators")]
        public string[] GapIndicators { get; set; } = [];
    }
}
