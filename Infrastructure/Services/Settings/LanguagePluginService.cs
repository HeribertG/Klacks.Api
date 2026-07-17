// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade for language plugin management: discovery, installation, uninstallation and translations.
/// Delegates geo data operations to <see cref="LanguagePluginGeoDataInstaller"/>
/// and content operations to <see cref="LanguagePluginContentInstaller"/>.
/// </summary>
/// <param name="scopeFactory">Factory for DI scopes in database operations</param>
/// <param name="configuration">App configuration for the plugin directory</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginService : ILanguagePluginService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LanguagePluginService> _logger;
    private readonly string _pluginDirectory;
    private readonly ConcurrentDictionary<string, LanguagePluginManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _translationCache = new();
    private readonly HashSet<string> _installedCodes = new();
    private readonly object _installedLock = new();
    private bool _initialized;

    private readonly LanguagePluginGeoDataInstaller _geoDataInstaller;
    private readonly LanguagePluginContentInstaller _contentInstaller;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LanguagePluginService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<LanguagePluginService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var configuredDir = configuration.GetValue<string>("LanguagePlugins:Directory")
            ?? LanguagePluginConstants.PluginDirectory;

        _pluginDirectory = Path.IsPathRooted(configuredDir)
            ? configuredDir
            : Path.Combine(AppContext.BaseDirectory, configuredDir);

        _geoDataInstaller = new LanguagePluginGeoDataInstaller(_pluginDirectory, _manifests, _logger);
        _contentInstaller = new LanguagePluginContentInstaller(_pluginDirectory, _logger);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        DiscoverPlugins();
        await LoadInstalledCodesFromDatabaseAsync();
        await BackfillDefaultGeoTranslationsAsync();
        await BackfillDocsAsync();
        await BackfillCountriesAsync();
        _initialized = true;
    }

    private async Task BackfillDefaultGeoTranslationsAsync()
    {
        string[] codes;
        lock (_installedLock)
        {
            codes = _installedCodes.ToArray();
        }

        if (codes.Length == 0)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            foreach (var code in codes)
            {
                await _contentInstaller.MergeDefaultGeoTranslationsAsync(scope, code);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backfill default geo translations for installed language plugins");
        }
    }

    /// <summary>
    /// Re-syncs manual docs from the plugin directory into the database for every already-installed
    /// language on each startup, so manuals added to a plugin after its initial install are picked up
    /// without requiring an uninstall/reinstall cycle.
    /// </summary>
    private async Task BackfillDocsAsync()
    {
        string[] codes;
        lock (_installedLock)
        {
            codes = _installedCodes.ToArray();
        }

        if (codes.Length == 0)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            foreach (var code in codes)
            {
                await _contentInstaller.InstallDocsAsync(scope, code);
            }

            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backfill docs for installed language plugins");
        }
    }

    /// <summary>
    /// Re-runs the plugin country upsert for every already-installed language on each startup, so a
    /// plugin's home country is picked up even if it was added to countries.json after the plugin
    /// was originally installed.
    /// </summary>
    private async Task BackfillCountriesAsync()
    {
        string[] codes;
        lock (_installedLock)
        {
            codes = _installedCodes.ToArray();
        }

        if (codes.Length == 0)
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            foreach (var code in codes)
            {
                await _contentInstaller.InstallCountryAsync(scope, code);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backfill countries for installed language plugins");
        }
    }

    public IReadOnlyList<LanguagePluginInfo> GetAllPlugins()
    {
        var plugins = new List<LanguagePluginInfo>();

        foreach (var coreCode in LanguagePluginConstants.CoreLanguages)
        {
            plugins.Add(new LanguagePluginInfo
            {
                Code = coreCode,
                Name = coreCode,
                DisplayName = coreCode.ToUpperInvariant(),
                Direction = "ltr",
                IsCore = true,
                IsInstalled = true
            });
        }

        foreach (var kvp in _manifests)
        {
            var manifest = kvp.Value;
            var translations = GetTranslations(kvp.Key);

            plugins.Add(new LanguagePluginInfo
            {
                Code = manifest.Code,
                Name = manifest.Name,
                DisplayName = manifest.DisplayName,
                SpeechLocale = manifest.SpeechLocale,
                Version = manifest.Version,
                Author = manifest.Author,
                Coverage = manifest.Coverage,
                MinKlacksVersion = manifest.MinKlacksVersion,
                Direction = manifest.Direction,
                IsCore = false,
                IsInstalled = IsInstalled(manifest.Code),
                TranslationCount = translations?.Count ?? 0
            });
        }

        return plugins;
    }

    public LanguagePluginInfo? GetPlugin(string code)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(code))
        {
            return new LanguagePluginInfo
            {
                Code = code,
                Name = code,
                DisplayName = code.ToUpperInvariant(),
                Direction = "ltr",
                IsCore = true,
                IsInstalled = true
            };
        }

        if (!_manifests.TryGetValue(code, out var manifest))
            return null;

        var translations = GetTranslations(code);

        return new LanguagePluginInfo
        {
            Code = manifest.Code,
            Name = manifest.Name,
            DisplayName = manifest.DisplayName,
            SpeechLocale = manifest.SpeechLocale,
            Version = manifest.Version,
            Author = manifest.Author,
            Coverage = manifest.Coverage,
            MinKlacksVersion = manifest.MinKlacksVersion,
            Direction = manifest.Direction,
            IsCore = false,
            IsInstalled = IsInstalled(code),
            TranslationCount = translations?.Count ?? 0
        };
    }

    public async Task<bool> InstallAsync(string code)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(code))
            return false;

        if (!_manifests.TryGetValue(code, out var manifest))
            return false;

        if (!IsVersionCompatible(manifest.MinKlacksVersion))
        {
            _logger.LogWarning(
                "Language plugin '{Code}' requires Klacks version {MinVersion}, but current version is {CurrentVersion}",
                code.ForLog(), manifest.MinKlacksVersion.ForLog(), $"{MyVersion.Major}.{MyVersion.Minor}.{MyVersion.Patch}");
            return false;
        }

        var settingKey = LanguagePluginConstants.SettingPrefix + code.ToUpperInvariant();

        using var scope = _scopeFactory.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = await settingsRepo.GetSetting(settingKey);
        if (existing != null)
        {
            existing.Value = "true";
        }
        else
        {
            await settingsRepo.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = settingKey,
                Value = "true"
            });
        }

        await _geoDataInstaller.InstallGeoDataAsync(scope, code);
        await _contentInstaller.InstallDocsAsync(scope, code);
        await _contentInstaller.InstallSkillSynonymsAsync(scope, code);
        await _contentInstaller.InstallRecipeSynonymsAsync(scope, code);
        await _contentInstaller.InstallNavigationSynonymsAsync(scope, code);
        await _contentInstaller.InstallSentimentKeywordsAsync(scope, code);
        await _contentInstaller.InstallWakeWordsAsync(code);
        await unitOfWork.CompleteAsync();
        await _contentInstaller.MergeNonCoreTranslationsAsync(scope, code);
        await _contentInstaller.MergeDefaultGeoTranslationsAsync(scope, code);
        await _contentInstaller.InstallCountryAsync(scope, code);

        lock (_installedLock)
        {
            _installedCodes.Add(code);
        }

        _logger.LogInformation("Language plugin '{Code}' installed", code.ForLog());
        return true;
    }

    public async Task<bool> UninstallAsync(string code)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(code))
            return false;

        var settingKey = LanguagePluginConstants.SettingPrefix + code.ToUpperInvariant();

        using var scope = _scopeFactory.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await _contentInstaller.UninstallSkillSynonymsAsync(scope, code);
        await _contentInstaller.UninstallRecipeSynonymsAsync(scope, code);
        await _contentInstaller.UninstallNavigationSynonymsAsync(scope, code);
        await _contentInstaller.UninstallSentimentKeywordsAsync(scope, code);
        await _geoDataInstaller.UninstallGeoDataAsync(scope, code);
        await _contentInstaller.UninstallDocsAsync(scope, code);
        await _contentInstaller.RemoveDefaultGeoTranslationsAsync(scope, code);

        var existing = await settingsRepo.GetSetting(settingKey);
        if (existing != null)
        {
            existing.Value = "false";
        }

        await unitOfWork.CompleteAsync();

        lock (_installedLock)
        {
            _installedCodes.Remove(code);
        }

        _logger.LogInformation("Language plugin '{Code}' uninstalled", code.ForLog());
        return true;
    }

    public Dictionary<string, string>? GetTranslations(string code)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(code))
            return null;

        if (!IsInstalled(code))
            return null;

        if (_translationCache.TryGetValue(code, out var cached))
            return cached;

        var translationsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.TranslationsFileName);
        if (!File.Exists(translationsPath))
            return null;

        try
        {
            var json = File.ReadAllText(translationsPath);
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);

            if (translations != null)
            {
                _translationCache[code] = translations;
            }

            return translations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load translations for language plugin '{Code}'", code.ForLog());
            return null;
        }
    }

    public IReadOnlyList<string> GetInstalledPluginCodes()
    {
        lock (_installedLock)
        {
            return _installedCodes.ToList();
        }
    }

    public async Task<string?> GetPluginDocAsync(string code, string manualName)
    {
        if (!IsInstalled(code))
            return null;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var doc = await db.PluginDocs
            .FirstOrDefaultAsync(d => d.PluginCode == code && d.ManualName == manualName);

        return doc?.HtmlContent;
    }

    public async Task RefreshPluginsAsync()
    {
        _manifests.Clear();
        _translationCache.Clear();
        _initialized = false;
        await InitializeAsync();
    }

    private static bool IsVersionCompatible(string minVersion)
    {
        if (string.IsNullOrWhiteSpace(minVersion))
            return true;

        var parts = minVersion.Split('.');
        if (parts.Length != 3
            || !int.TryParse(parts[0], out var reqMajor)
            || !int.TryParse(parts[1], out var reqMinor)
            || !int.TryParse(parts[2], out var reqPatch))
        {
            return true;
        }

        var currentVersion = new Version(MyVersion.Major, MyVersion.Minor, MyVersion.Patch);
        var requiredVersion = new Version(reqMajor, reqMinor, reqPatch);

        return currentVersion >= requiredVersion;
    }

    private bool IsInstalled(string code)
    {
        lock (_installedLock)
        {
            return _installedCodes.Contains(code);
        }
    }

    private void DiscoverPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            _logger.LogInformation("Language plugins directory not found at '{Path}', skipping discovery", _pluginDirectory);
            return;
        }

        var directories = Directory.GetDirectories(_pluginDirectory);

        foreach (var dir in directories)
        {
            var manifestPath = Path.Combine(dir, LanguagePluginConstants.ManifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<LanguagePluginManifest>(json, JsonOptions);

                if (manifest == null || string.IsNullOrWhiteSpace(manifest.Code))
                    continue;

                if (LanguagePluginConstants.CoreLanguages.Contains(manifest.Code))
                {
                    _logger.LogWarning("Language plugin '{Code}' conflicts with core language, skipping", manifest.Code);
                    continue;
                }

                _manifests[manifest.Code] = manifest;
                _logger.LogDebug("Discovered language plugin: {Code} ({DisplayName})", manifest.Code, manifest.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load manifest from '{Path}'", manifestPath);
            }
        }

        _logger.LogInformation("Discovered {Count} language plugin(s)", _manifests.Count);
    }

    private async Task LoadInstalledCodesFromDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var settings = await settingsRepo.GetSettingsList();

            lock (_installedLock)
            {
                _installedCodes.Clear();

                foreach (var setting in settings)
                {
                    if (!setting.Type.StartsWith(LanguagePluginConstants.SettingPrefix))
                        continue;

                    if (!string.Equals(setting.Value, "true", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var code = setting.Type[LanguagePluginConstants.SettingPrefix.Length..].ToLowerInvariant();

                    if (_manifests.ContainsKey(code))
                    {
                        _installedCodes.Add(code);
                    }
                }
            }

            _logger.LogInformation("Loaded {Count} installed language plugin(s) from database", _installedCodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load installed language plugins from database");
        }
    }
}
