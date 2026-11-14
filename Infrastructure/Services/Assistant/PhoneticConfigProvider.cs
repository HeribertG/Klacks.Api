// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads per-language phonetic configuration. Core languages (de/en/fr/it) come from
/// the single Application/Klacksy/phonetics-core.json; plugin languages come from their
/// own Plugins/Languages/{locale}/phonetics.json. Missing config falls back to a safe
/// default. Mirrors the UtteranceNormalizer pack-loading pattern; results are cached.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Assistant;

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Klacksy;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class PhoneticConfigProvider : IPhoneticConfigProvider
{
    private const string DefaultCoreLocale = "de";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, PhoneticConfig> _core;
    private readonly ConcurrentDictionary<string, PhoneticConfig> _pluginCache = new();
    private readonly string _pluginDirectory;
    private readonly ILogger<PhoneticConfigProvider> _logger;

    public PhoneticConfigProvider(string? configuredPluginDirectory = null, ILogger<PhoneticConfigProvider>? logger = null)
    {
        _logger = logger ?? NullLogger<PhoneticConfigProvider>.Instance;
        _pluginDirectory = LanguagePluginDirectoryResolver.Resolve(AppContext.BaseDirectory, configuredPluginDirectory);
        _core = LoadCore();
    }

    public PhoneticConfig GetForLocale(string? locale)
    {
        var lang = Normalize(locale);
        if (LanguagePluginConstants.CoreLanguages.Contains(lang))
        {
            return _core.TryGetValue(lang, out var config) ? config : new PhoneticConfig();
        }

        return _pluginCache.GetOrAdd(lang, LoadPlugin);
    }

    private PhoneticConfig LoadPlugin(string lang)
    {
        try
        {
            if (!TryResolvePluginDirectory(lang, out var languageDirectory))
            {
                _logger.LogWarning("Skipping phonetic config for locale outside the plugin directory: {Locale}", LogSanitizer.ForLog(lang));
                return new PhoneticConfig();
            }

            var path = Path.Combine(languageDirectory, LanguagePluginConstants.PhoneticsFileName);
            if (!File.Exists(path))
                return new PhoneticConfig();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PhoneticConfig>(json, JsonOptions) ?? new PhoneticConfig();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loading the phonetic config for locale {Locale} failed, using the default", LogSanitizer.ForLog(lang));
            return new PhoneticConfig();
        }
    }

    private bool TryResolvePluginDirectory(string lang, out string languageDirectory)
    {
        languageDirectory = string.Empty;

        if (string.IsNullOrEmpty(lang) || !string.Equals(Path.GetFileName(lang), lang, StringComparison.Ordinal))
            return false;

        var basePath = Path.GetFullPath(_pluginDirectory);
        var baseWithSeparator = basePath.EndsWith(Path.DirectorySeparatorChar)
            ? basePath
            : basePath + Path.DirectorySeparatorChar;

        var candidate = Path.GetFullPath(Path.Combine(basePath, lang));
        if (!candidate.StartsWith(baseWithSeparator, StringComparison.Ordinal))
            return false;

        languageDirectory = candidate;
        return true;
    }

    private static string Normalize(string? locale)
    {
        return LanguageTag.BaseLanguage(locale)?.ToLowerInvariant() ?? DefaultCoreLocale;
    }

    private static Dictionary<string, PhoneticConfig> LoadCore()
    {
        try
        {
            var path = Path.Combine(
                AppContext.BaseDirectory,
                LanguagePluginConstants.ApplicationDirectory,
                LanguagePluginConstants.KlacksyDirectory,
                LanguagePluginConstants.PhoneticsCoreFileName);
            if (!File.Exists(path))
                return new Dictionary<string, PhoneticConfig>(StringComparer.OrdinalIgnoreCase);

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, PhoneticConfig>>(json, JsonOptions)
                   ?? new Dictionary<string, PhoneticConfig>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, PhoneticConfig>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
