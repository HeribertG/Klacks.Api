// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads conversation-signals.json from each installed language plugin directory and passes the
/// affirmation, negation, correction, cancellation, gap-indicator and decline entries to the
/// respective static detectors (AffirmationDetector, ImplicitCorrectionDetector,
/// RecipeCancellationDetector, RefusalSignalDetector, DeclineDetector). Called once at application
/// startup; plugin languages extend the core detection so confirmations, aborts and refusals are
/// understood in every supported language. LoadCore installs the core-language vocabulary from
/// conversation-signals-core.json before the packs are read. Every pack directory is read, whether or not the
/// language is installed: the packs are build content, and the detectors merge them into one global vocabulary.
/// The per-language lists are also handed to ConversationSignalCollisions, which generates the table of words one
/// language lists as a yes and another as a no; a pack that cannot be read is reported and leaves the table
/// without that language, so a collision it would introduce is not resolved either. Install is the single place that
/// wires already-read signals into the detectors and the collision holder; Load is ReadCore + ReadPacks + Install, and the
/// unit tests call Install on the shipped files instead of copying the wiring.
/// </summary>
/// <param name="baseDirectory">Application base directory containing the Plugins folder</param>
/// <param name="onError">Optional callback invoked per plugin file that failed to load</param>
/// <param name="configuredPluginDirectory">Value of LanguagePlugins:Directory, null when not configured</param>
/// <param name="logger">Optional logger: receives a warning per pack that could not be read and the generated collision table as information</param>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Klacksy;

public static class ConversationSignalsPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string CoreSignalsMissingMessage =
        "The core conversation-signals file is missing from the deployment, so the core refusal "
        + "vocabulary is empty and no core-language decline is detected at all";

    private const string PackSkippedMessage =
        "The language pack was skipped: its vocabulary is not active and the collision table is generated without it";

    private const string LoadFailedLogTemplate = "Language plugin file failed to load: {File}";
    private const string CollisionTableLogTemplate = "{CollisionTable}";

    public static void Load(
        string baseDirectory,
        Action<string, Exception>? onError = null,
        string? configuredPluginDirectory = null,
        ILogger? logger = null)
    {
        void Report(string file, Exception ex)
        {
            onError?.Invoke(file, ex);
            logger?.LogWarning(ex, LoadFailedLogTemplate, file);
        }

        var core = ReadCore(baseDirectory, Report);
        var packs = ReadPacks(baseDirectory, Report, configuredPluginDirectory);

        Install(core, packs);
        logger?.LogInformation(CollisionTableLogTemplate, ConversationSignalCollisions.Current.Describe());
    }

    /// <summary>
    /// The one wiring of already-read signals into the process: the core refusal vocabulary into DeclineDetector, the
    /// per-language lists into the collision holder, and the merged pack lists into every static detector. Load runs
    /// it at startup and the tests run it on the shipped files, so a test sees exactly the detectors production has.
    /// Additive on process-wide static state, like the Configure methods it calls.
    /// </summary>
    /// <param name="core">Core-language signals as ReadCore returns them</param>
    /// <param name="packs">Language-pack signals as ReadPacks returns them</param>
    public static void Install(
        IReadOnlyList<ConversationLanguageSignals> core, IReadOnlyList<ConversationLanguageSignals> packs)
    {
        InstallCore(core);
        ConversationSignalCollisions.Register(core.Concat(packs));

        var affirmations = packs.SelectMany(pack => pack.Affirmations).ToList();
        var negations = packs.SelectMany(pack => pack.Negations).ToList();
        var corrections = packs.SelectMany(pack => pack.Corrections).ToList();
        var cancellations = packs.SelectMany(pack => pack.Cancellations).ToList();
        var gapIndicators = packs.SelectMany(pack => pack.GapIndicators).ToList();
        var declines = packs.SelectMany(pack => pack.Declines).ToList();

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
    /// Reads the conversation-signals.json of every language pack directory without configuring any detector, so the
    /// per-language lists the collision table is generated from can be obtained the way production obtains them. A pack
    /// that cannot be read is reported through onError and left out.
    /// </summary>
    /// <param name="baseDirectory">Application base directory containing the Plugins folder</param>
    /// <param name="onError">Optional callback invoked per plugin file that failed to load</param>
    /// <param name="configuredPluginDirectory">Value of LanguagePlugins:Directory, null when not configured</param>
    public static IReadOnlyList<ConversationLanguageSignals> ReadPacks(
        string baseDirectory, Action<string, Exception>? onError = null, string? configuredPluginDirectory = null)
    {
        var pluginDir = LanguagePluginDirectoryResolver.Resolve(baseDirectory, configuredPluginDirectory);
        if (!Directory.Exists(pluginDir))
            return [];

        var packs = new List<ConversationLanguageSignals>();

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
                if (data == null)
                {
                    onError?.Invoke(file, new InvalidDataException(PackSkippedMessage));
                    continue;
                }

                packs.Add(ToLanguageSignals(code, data));
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, new InvalidDataException(PackSkippedMessage, ex));
            }
        }

        return packs;
    }

    /// <summary>
    /// Installs the core-language vocabulary, which lives in conversation-signals-core.json next to the
    /// application rather than in a language plugin: the core languages ship no plugin directory at all,
    /// and ReadPacks deliberately skips them so a plugin can never overwrite what the application
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
    public static IReadOnlyList<ConversationLanguageSignals> LoadCore(
        string baseDirectory, Action<string, Exception>? onError = null)
    {
        var core = ReadCore(baseDirectory, onError);
        InstallCore(core);

        return core;
    }

    private static void InstallCore(IReadOnlyList<ConversationLanguageSignals> core)
    {
        var negations = core.SelectMany(signals => signals.Negations).ToList();
        if (negations.Count > 0)
            DeclineDetector.ConfigureCore(negations);
    }

    /// <summary>
    /// Reads the core-language negations of conversation-signals-core.json per language without configuring any
    /// detector. A missing or unreadable file is reported through onError and yields an empty list.
    /// </summary>
    /// <param name="baseDirectory">Application base directory containing the core signals file</param>
    /// <param name="onError">Optional callback invoked when the core file is missing or failed to load</param>
    public static IReadOnlyList<ConversationLanguageSignals> ReadCore(
        string baseDirectory, Action<string, Exception>? onError = null)
    {
        var file = Path.Combine(
            baseDirectory,
            LanguagePluginConstants.ApplicationDirectory,
            LanguagePluginConstants.KlacksyDirectory,
            LanguagePluginConstants.ConversationSignalsCoreFileName);

        if (!File.Exists(file))
        {
            onError?.Invoke(file, new FileNotFoundException(CoreSignalsMissingMessage, file));
            return [];
        }

        try
        {
            var json = File.ReadAllText(file);
            var byLanguage = JsonSerializer.Deserialize<Dictionary<string, ConversationSignalsData>>(json, JsonOptions);
            if (byLanguage == null)
                return [];

            return byLanguage
                .Select(pair => new ConversationLanguageSignals(pair.Key) { Negations = pair.Value.Negations })
                .ToList();
        }
        catch (Exception ex)
        {
            onError?.Invoke(file, ex);
            return [];
        }
    }

    private static ConversationLanguageSignals ToLanguageSignals(string language, ConversationSignalsData data) =>
        new(language)
        {
            Affirmations = data.Affirmations,
            Negations = data.Negations,
            Declines = data.Declines,
            Cancellations = data.Cancellations,
            Corrections = data.Corrections,
            GapIndicators = data.GapIndicators
        };

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
