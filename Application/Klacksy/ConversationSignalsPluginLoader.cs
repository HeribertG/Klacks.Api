// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads conversation-signals.json from each installed language plugin directory and passes the
/// affirmation, negation, correction, cancellation, gap-indicator and decline entries to the
/// respective static detectors (AffirmationDetector, ImplicitCorrectionDetector,
/// RecipeCancellationDetector, RefusalSignalDetector, DeclineDetector). Called once at application
/// startup; plugin languages extend the core detection so confirmations, aborts and refusals are
/// understood in every supported language. LoadCore installs the core-language vocabulary from
/// conversation-signals-core.json before the packs are read.
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

    private const string CoreSignalsMissingMessage =
        "The core conversation-signals file is missing from the deployment, so the core refusal "
        + "vocabulary is empty and no core-language decline is detected at all";

    public static void Load(string baseDirectory, Action<string, Exception>? onError = null)
    {
        LoadCore(baseDirectory, onError);

        var pluginDir = Path.Combine(baseDirectory, LanguagePluginConstants.PluginDirectory);
        if (!Directory.Exists(pluginDir))
            return;

        var affirmations = new List<string>();
        var negations = new List<string>();
        var corrections = new List<string>();
        var cancellations = new List<string>();
        var gapIndicators = new List<string>();
        var declines = new List<string>();

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
                declines.AddRange(data.Declines);
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, ex);
            }
        }

        if (affirmations.Count > 0 || negations.Count > 0)
            AffirmationDetector.Configure(affirmations, negations);

        if (negations.Count > 0 || declines.Count > 0)
            DeclineDetector.Configure(negations, declines);

        if (corrections.Count > 0)
            ImplicitCorrectionDetector.Configure(corrections);

        if (cancellations.Count > 0)
            RecipeCancellationDetector.Configure(cancellations);

        if (gapIndicators.Count > 0)
            RefusalSignalDetector.Configure(gapIndicators);
    }

    /// <summary>
    /// Installs the core-language vocabulary, which lives in conversation-signals-core.json next to the
    /// application rather than in a language plugin: the core languages ship no plugin directory at all,
    /// and the loop above deliberately skips them so a plugin can never overwrite what the application
    /// ships. Only the decline vocabulary has moved out of the code so far, so only DeclineDetector is
    /// fed here; the remaining detectors still carry their core tokens.
    /// Public and separate from Load so a caller can install the core vocabulary without the language
    /// packs - plugin entries are additive, process-wide static state, and pulling them in where they are
    /// not wanted changes the outcome of everything that runs afterwards.
    /// A missing file is reported through onError rather than returned from silently: the vocabulary is
    /// data that only reaches the output directory through a copy entry in the project file, so losing it
    /// disables every core-language refusal at once, and the silent return made that indistinguishable
    /// from a healthy start.
    /// </summary>
    /// <param name="baseDirectory">Application base directory containing the core signals file</param>
    /// <param name="onError">Optional callback invoked when the core file is missing or failed to load</param>
    public static void LoadCore(string baseDirectory, Action<string, Exception>? onError = null)
    {
        var file = Path.Combine(
            baseDirectory,
            LanguagePluginConstants.ApplicationDirectory,
            LanguagePluginConstants.KlacksyDirectory,
            LanguagePluginConstants.ConversationSignalsCoreFileName);

        if (!File.Exists(file))
        {
            onError?.Invoke(file, new FileNotFoundException(CoreSignalsMissingMessage, file));
            return;
        }

        try
        {
            var json = File.ReadAllText(file);
            var byLanguage = JsonSerializer.Deserialize<Dictionary<string, ConversationSignalsData>>(json, JsonOptions);
            if (byLanguage == null)
                return;

            var negations = byLanguage.Values.SelectMany(signals => signals.Negations).ToList();
            if (negations.Count > 0)
                DeclineDetector.ConfigureCore(negations);
        }
        catch (Exception ex)
        {
            onError?.Invoke(file, ex);
        }
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

        [JsonPropertyName("declines")]
        public string[] Declines { get; set; } = [];
    }
}
