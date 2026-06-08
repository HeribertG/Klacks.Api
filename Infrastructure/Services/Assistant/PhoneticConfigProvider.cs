// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class PhoneticConfigProvider : IPhoneticConfigProvider
{
    private const string DefaultCoreLocale = "de";

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly Dictionary<string, PhoneticConfig> _core;
    private readonly ConcurrentDictionary<string, PhoneticConfig> _pluginCache = new();

    public PhoneticConfigProvider()
    {
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

    private static string Normalize(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
            return DefaultCoreLocale;

        var value = locale.Trim().ToLowerInvariant();
        var separator = value.IndexOf('-');
        return separator > 0 ? value[..separator] : value;
    }

    private static Dictionary<string, PhoneticConfig> LoadCore()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Application", "Klacksy", LanguagePluginConstants.PhoneticsCoreFileName);
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

    private static PhoneticConfig LoadPlugin(string lang)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, LanguagePluginConstants.PluginDirectory, lang, LanguagePluginConstants.PhoneticsFileName);
            if (!File.Exists(path))
                return new PhoneticConfig();

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<PhoneticConfig>(json, JsonOptions) ?? new PhoneticConfig();
        }
        catch
        {
            return new PhoneticConfig();
        }
    }
}
