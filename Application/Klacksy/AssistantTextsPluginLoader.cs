// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the language-pack texts the server writes WITHOUT a model call into their catalogues, from each
/// installed language plugin directory. assistant-texts.json feeds GracefulCorrectionTexts,
/// ClarificationTexts (the planner notices, status words and reply subject of the inbound clarification
/// dialog) and EscalationHandoffTexts (the escalation confirmations and quiet notes): no prompt rule can
/// translate these, so the pack has to carry them - unlike conversation-signals.json, which carries
/// input-side vocabulary. translations.json, the frontend catalogue, feeds MessengerProactiveTexts with the
/// five assistant.proactive.* sentences a messenger can carry: the pack already ships them for the inbox,
/// and reading that one source keeps inbox and messenger identical. Called once at application startup
/// next to the other pack loaders; a pack installed while the process runs takes effect on the next
/// restart, exactly like every other loader there. A pack directory without assistant-texts.json is skipped
/// and reported through onMissingFile: without the file the language counts as unknown to those catalogues
/// and its server-written texts go out in English without any warning. The same holds for a pack whose
/// translations.json is missing or carries none of the proactive keys; a pack that carries only some of
/// them is warned about at lookup time, and the catalogue guard keeps both from shipping.
/// </summary>
/// <param name="baseDirectory">Application base directory containing the Plugins folder</param>
/// <param name="onError">Optional callback invoked per plugin file that failed to load</param>
/// <param name="onMissingFile">Optional callback invoked with the language code of each installed pack directory that has no assistant-texts.json</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Klacksy;

public static class AssistantTextsPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void Load(
        string baseDirectory, Action<string, Exception>? onError = null, Action<string>? onMissingFile = null)
    {
        var pluginDir = Path.Combine(baseDirectory, LanguagePluginConstants.PluginDirectory);
        if (!Directory.Exists(pluginDir))
        {
            return;
        }

        foreach (var langDir in Directory.GetDirectories(pluginDir))
        {
            var code = Path.GetFileName(langDir);
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
            {
                continue;
            }

            LoadAssistantTexts(langDir, code, onError, onMissingFile);
            LoadProactiveTexts(langDir, code, onError);
        }
    }

    private static void LoadAssistantTexts(
        string langDir, string code, Action<string, Exception>? onError, Action<string>? onMissingFile)
    {
        var file = Path.Combine(langDir, LanguagePluginConstants.AssistantTextsFileName);
        if (!File.Exists(file))
        {
            onMissingFile?.Invoke(code);
            return;
        }

        try
        {
            var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(file), JsonOptions);
            if (texts is { Count: > 0 })
            {
                GracefulCorrectionTexts.Configure(code, texts);
                ClarificationTexts.Configure(code, texts);
                EscalationHandoffTexts.Configure(code, texts);
            }
        }
        catch (Exception ex)
        {
            onError?.Invoke(file, ex);
        }
    }

    private static void LoadProactiveTexts(string langDir, string code, Action<string, Exception>? onError)
    {
        var file = Path.Combine(langDir, LanguagePluginConstants.TranslationsFileName);
        if (!File.Exists(file))
        {
            return;
        }

        try
        {
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(file), JsonOptions);
            if (translations is null)
            {
                return;
            }

            var proactive = MessengerProactiveTexts.CoveredKeys
                .Where(translations.ContainsKey)
                .ToDictionary(key => key, key => translations[key], StringComparer.Ordinal);
            if (proactive.Count > 0)
            {
                MessengerProactiveTexts.Configure(code, proactive);
            }
        }
        catch (Exception ex)
        {
            onError?.Invoke(file, ex);
        }
    }
}
