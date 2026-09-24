// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads assistant-texts.json from each installed language plugin directory into
/// GracefulCorrectionTexts and ClarificationTexts (the planner notices, status words and reply subject of the
/// inbound clarification dialog). These are the few sentences the assistant sends WITHOUT a model call, so no
/// prompt rule can translate them and the pack has to carry them - unlike conversation-signals.json,
/// which carries input-side vocabulary, and unlike translations.json, which is the frontend catalogue.
/// Called once at application startup next to the other pack loaders; a pack installed while the
/// process runs takes effect on the next restart, exactly like every other loader there. A pack directory
/// without assistant-texts.json is skipped and reported through onMissingFile: without the file the language
/// counts as unknown to the catalogues and its server-written texts go out in English without any warning.
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

            var file = Path.Combine(langDir, LanguagePluginConstants.AssistantTextsFileName);
            if (!File.Exists(file))
            {
                onMissingFile?.Invoke(code);
                continue;
            }

            try
            {
                var texts = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(file), JsonOptions);
                if (texts is { Count: > 0 })
                {
                    GracefulCorrectionTexts.Configure(code, texts);
                    ClarificationTexts.Configure(code, texts);
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, ex);
            }
        }
    }
}
