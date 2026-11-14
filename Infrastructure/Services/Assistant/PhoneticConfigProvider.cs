// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads per-language phonetic configuration. Core languages (de/en/fr/it) come from
/// the single Application/Klacksy/phonetics-core.json; plugin languages come from their
/// own Plugins/Languages/{locale}/phonetics.json. Missing config or a locale that is not a valid
/// pack code falls back to a safe default; only configs read from an existing pack file are cached, and a
/// config that failed to load is not remembered, so the next call retries.
/// Mirrors the UtteranceNormalizer pack-loading pattern.
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
    private readonly ConcurrentDictionary<string, PhoneticConfig> _pluginCache = new(StringComparer.OrdinalIgnoreCase);
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

        if (!LanguagePluginCode.IsValid(lang))
        {
            _logger.LogDebug("Skipping phonetic config for an invalid locale: {Locale}", LogSanitizer.ForLog(lang));
            return new PhoneticConfig();
        }

        if (_pluginCache.TryGetValue(lang, out var cached))
            return cached;

        var path = Path.Combine(_pluginDirectory, lang, LanguagePluginConstants.PhoneticsFileName);
        if (!File.Exists(path))
            return new PhoneticConfig();

        var loaded = LoadPlugin(path, lang);
        return loaded == null ? new PhoneticConfig() : _pluginCache.GetOrAdd(lang, loaded);
    }

    private PhoneticConfig? LoadPlugin(string path, string lang)
    {
        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PhoneticConfig>(json, JsonOptions) ?? new PhoneticConfig();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Loading the phonetic config for locale {Locale} failed, using the default", LogSanitizer.ForLog(lang));
            return null;
        }
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
