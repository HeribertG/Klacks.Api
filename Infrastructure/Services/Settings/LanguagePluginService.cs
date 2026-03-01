// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginService : ILanguagePluginService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LanguagePluginService> _logger;
    private readonly string _pluginDirectory;
    private readonly ConcurrentDictionary<string, LanguagePluginManifest> _manifests = new();
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _translationCache = new();
    private readonly HashSet<string> _installedCodes = new();
    private readonly object _installedLock = new();
    private bool _initialized;

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
    }

    public void Initialize()
    {
        if (_initialized) return;

        DiscoverPlugins();
        LoadInstalledCodesFromDatabase();
        _initialized = true;
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
            IsCore = false,
            IsInstalled = IsInstalled(code),
            TranslationCount = translations?.Count ?? 0
        };
    }

    public async Task<bool> InstallAsync(string code)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(code))
            return false;

        if (!_manifests.ContainsKey(code))
            return false;

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

        await unitOfWork.CompleteAsync();

        lock (_installedLock)
        {
            _installedCodes.Add(code);
        }

        _logger.LogInformation("Language plugin '{Code}' installed", code);
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

        var existing = await settingsRepo.GetSetting(settingKey);
        if (existing != null)
        {
            existing.Value = "false";
            await unitOfWork.CompleteAsync();
        }

        lock (_installedLock)
        {
            _installedCodes.Remove(code);
        }

        _logger.LogInformation("Language plugin '{Code}' uninstalled", code);
        return true;
    }

    public Dictionary<string, string>? GetTranslations(string code)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(code))
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
            _logger.LogError(ex, "Failed to load translations for language plugin '{Code}'", code);
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

    public void RefreshPlugins()
    {
        _manifests.Clear();
        _translationCache.Clear();
        _initialized = false;
        Initialize();
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

    private void LoadInstalledCodesFromDatabase()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var settings = settingsRepo.GetSettingsList().GetAwaiter().GetResult();

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
