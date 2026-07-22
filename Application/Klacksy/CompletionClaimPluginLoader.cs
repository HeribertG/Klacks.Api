// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads completion-claim.json from each installed language plugin directory and passes the
/// completion-claim phrases to CompletionClaimDetector.Configure().
/// Called once at application startup; plugin languages extend the core de/en/fr/it detection.
/// </summary>
/// <param name="baseDirectory">Application base directory containing the Plugins folder</param>
/// <param name="onError">Optional callback invoked per plugin file that failed to load</param>

using System.Text.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Klacksy;

public static class CompletionClaimPluginLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static void Load(string baseDirectory, Action<string, Exception>? onError = null)
    {
        var pluginDir = Path.Combine(baseDirectory, LanguagePluginConstants.PluginDirectory);
        if (!Directory.Exists(pluginDir))
            return;

        var allClaims = new List<string>();

        foreach (var langDir in Directory.GetDirectories(pluginDir))
        {
            var code = Path.GetFileName(langDir);
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
                continue;

            var file = Path.Combine(langDir, LanguagePluginConstants.CompletionClaimFileName);
            if (!File.Exists(file))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<CompletionClaimData>(json, JsonOptions);
                if (data == null) continue;

                allClaims.AddRange(data.CompletionClaims);
            }
            catch (Exception ex)
            {
                onError?.Invoke(file, ex);
            }
        }

        if (allClaims.Count > 0)
            CompletionClaimDetector.Configure(allClaims);
    }

    private sealed class CompletionClaimData
    {
        [JsonPropertyName("completionClaims")]
        public string[] CompletionClaims { get; set; } = [];
    }
}
