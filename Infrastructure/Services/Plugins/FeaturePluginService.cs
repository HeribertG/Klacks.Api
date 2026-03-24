// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Singleton service for feature plugin lifecycle management: discovery, installation, enable/disable.
/// </summary>
/// <param name="scopeFactory">Factory for DI scopes in database operations</param>
/// <param name="configuration">App configuration for the plugin directory</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Plugins;

namespace Klacks.Api.Infrastructure.Services.Plugins;

public class FeaturePluginService : IFeaturePluginService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FeaturePluginService> _logger;
    private readonly string _pluginDirectory;
    private readonly ConcurrentDictionary<string, FeaturePluginManifest> _manifests = new();
    private readonly HashSet<string> _installedNames = new();
    private readonly HashSet<string> _enabledNames = new();
    private readonly object _installedLock = new();
    private readonly object _enabledLock = new();
    private bool _initialized;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public FeaturePluginService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<FeaturePluginService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

        var configuredDir = configuration.GetValue<string>("FeaturePlugins:Directory")
            ?? FeaturePluginConstants.PluginDirectory;

        _pluginDirectory = Path.IsPathRooted(configuredDir)
            ? configuredDir
            : Path.Combine(AppContext.BaseDirectory, configuredDir);
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        DiscoverPlugins();
        await LoadInstalledFromDatabaseAsync();
        _initialized = true;
    }

    public IReadOnlyList<FeaturePluginInfo> GetAllPlugins()
    {
        var plugins = new List<FeaturePluginInfo>();

        foreach (var kvp in _manifests)
        {
            var manifest = kvp.Value;

            plugins.Add(new FeaturePluginInfo
            {
                Name = manifest.Name,
                DisplayName = manifest.DisplayName,
                Category = manifest.Category,
                Version = manifest.Version,
                Author = manifest.Author,
                Description = manifest.Description,
                MinKlacksVersion = manifest.MinKlacksVersion,
                RequiredPermissions = manifest.RequiredPermissions,
                ProvidedSkills = manifest.ProvidedSkills,
                DefaultSettings = manifest.DefaultSettings,
                IsInstalled = IsInstalled(manifest.Name),
                IsEnabled = IsEnabled(manifest.Name),
                Navigation = manifest.Navigation
            });
        }

        return plugins;
    }

    public FeaturePluginInfo? GetPlugin(string name)
    {
        if (!_manifests.TryGetValue(name, out var manifest))
            return null;

        return new FeaturePluginInfo
        {
            Name = manifest.Name,
            DisplayName = manifest.DisplayName,
            Category = manifest.Category,
            Version = manifest.Version,
            Author = manifest.Author,
            Description = manifest.Description,
            MinKlacksVersion = manifest.MinKlacksVersion,
            RequiredPermissions = manifest.RequiredPermissions,
            ProvidedSkills = manifest.ProvidedSkills,
            DefaultSettings = manifest.DefaultSettings,
            IsInstalled = IsInstalled(name),
            IsEnabled = IsEnabled(name),
            Navigation = manifest.Navigation
        };
    }

    public async Task<bool> InstallAsync(string name)
    {
        if (!_manifests.TryGetValue(name, out var manifest))
            return false;

        if (!IsVersionCompatible(manifest.MinKlacksVersion))
        {
            _logger.LogWarning(
                "Feature plugin '{Name}' requires Klacks version {MinVersion}, but current version is {CurrentVersion}",
                name, manifest.MinKlacksVersion, $"{MyVersion.Major}.{MyVersion.Minor}.{MyVersion.Patch}");
            return false;
        }

        var settingKey = FeaturePluginConstants.SettingPrefix + name.ToUpperInvariant();
        var enabledKey = settingKey + FeaturePluginConstants.EnabledSuffix;

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

        var enabledSetting = await settingsRepo.GetSetting(enabledKey);
        if (enabledSetting != null)
        {
            enabledSetting.Value = "true";
        }
        else
        {
            await settingsRepo.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = enabledKey,
                Value = "true"
            });
        }

        await unitOfWork.CompleteAsync();

        lock (_installedLock)
        {
            _installedNames.Add(name);
        }

        lock (_enabledLock)
        {
            _enabledNames.Add(name);
        }

        _logger.LogInformation("Feature plugin '{Name}' installed", name);
        return true;
    }

    public async Task<bool> UninstallAsync(string name)
    {
        if (!_manifests.ContainsKey(name))
            return false;

        var settingKey = FeaturePluginConstants.SettingPrefix + name.ToUpperInvariant();
        var enabledKey = settingKey + FeaturePluginConstants.EnabledSuffix;

        using var scope = _scopeFactory.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = await settingsRepo.GetSetting(settingKey);
        if (existing != null)
        {
            existing.Value = "false";
        }

        var enabledSetting = await settingsRepo.GetSetting(enabledKey);
        if (enabledSetting != null)
        {
            enabledSetting.Value = "false";
        }

        await unitOfWork.CompleteAsync();

        lock (_installedLock)
        {
            _installedNames.Remove(name);
        }

        lock (_enabledLock)
        {
            _enabledNames.Remove(name);
        }

        _logger.LogInformation("Feature plugin '{Name}' uninstalled", name);
        return true;
    }

    public async Task<bool> EnableAsync(string name)
    {
        if (!IsInstalled(name))
            return false;

        var enabledKey = FeaturePluginConstants.SettingPrefix + name.ToUpperInvariant() + FeaturePluginConstants.EnabledSuffix;

        using var scope = _scopeFactory.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = await settingsRepo.GetSetting(enabledKey);
        if (existing != null)
        {
            existing.Value = "true";
        }
        else
        {
            await settingsRepo.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = enabledKey,
                Value = "true"
            });
        }

        await unitOfWork.CompleteAsync();

        lock (_enabledLock)
        {
            _enabledNames.Add(name);
        }

        _logger.LogInformation("Feature plugin '{Name}' enabled", name);
        return true;
    }

    public async Task<bool> DisableAsync(string name)
    {
        if (!IsInstalled(name))
            return false;

        var enabledKey = FeaturePluginConstants.SettingPrefix + name.ToUpperInvariant() + FeaturePluginConstants.EnabledSuffix;

        using var scope = _scopeFactory.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = await settingsRepo.GetSetting(enabledKey);
        if (existing != null)
        {
            existing.Value = "false";
        }

        await unitOfWork.CompleteAsync();

        lock (_enabledLock)
        {
            _enabledNames.Remove(name);
        }

        _logger.LogInformation("Feature plugin '{Name}' disabled", name);
        return true;
    }

    public bool IsEnabled(string name)
    {
        lock (_enabledLock)
        {
            return _enabledNames.Contains(name);
        }
    }

    public async Task RefreshPluginsAsync()
    {
        _manifests.Clear();
        _initialized = false;
        await InitializeAsync();
    }

    public Dictionary<string, string>? GetTranslations(string lang)
    {
        var merged = new Dictionary<string, string>();

        foreach (var kvp in _manifests)
        {
            if (!IsInstalled(kvp.Key) || !IsEnabled(kvp.Key))
                continue;

            var pluginDir = Path.Combine(_pluginDirectory, kvp.Key, FeaturePluginConstants.I18nDirectory);
            var filePath = Path.Combine(pluginDir, $"{lang}.json");

            if (!File.Exists(filePath))
                continue;

            try
            {
                var json = File.ReadAllText(filePath);
                var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);

                if (translations != null)
                {
                    foreach (var entry in translations)
                    {
                        merged[entry.Key] = entry.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load translations for plugin '{Plugin}' lang '{Lang}'", kvp.Key, lang);
            }
        }

        return merged.Count > 0 ? merged : null;
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

    private bool IsInstalled(string name)
    {
        lock (_installedLock)
        {
            return _installedNames.Contains(name);
        }
    }

    private void DiscoverPlugins()
    {
        if (!Directory.Exists(_pluginDirectory))
        {
            _logger.LogInformation("Feature plugins directory not found at '{Path}', skipping discovery", _pluginDirectory);
            return;
        }

        var directories = Directory.GetDirectories(_pluginDirectory);

        foreach (var dir in directories)
        {
            var manifestPath = Path.Combine(dir, FeaturePluginConstants.ManifestFileName);
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                var json = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<FeaturePluginManifest>(json, JsonOptions);

                if (manifest == null || string.IsNullOrWhiteSpace(manifest.Name))
                    continue;

                _manifests[manifest.Name] = manifest;
                _logger.LogDebug("Discovered feature plugin: {Name} ({DisplayName})", manifest.Name, manifest.DisplayName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load manifest from '{Path}'", manifestPath);
            }
        }

        _logger.LogInformation("Discovered {Count} feature plugin(s)", _manifests.Count);
    }

    private async Task LoadInstalledFromDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
            var settings = await settingsRepo.GetSettingsList();

            lock (_installedLock)
            {
                _installedNames.Clear();

                foreach (var setting in settings)
                {
                    if (!setting.Type.StartsWith(FeaturePluginConstants.SettingPrefix))
                        continue;

                    if (setting.Type.EndsWith(FeaturePluginConstants.EnabledSuffix))
                        continue;

                    if (!string.Equals(setting.Value, "true", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var name = setting.Type[FeaturePluginConstants.SettingPrefix.Length..].ToLowerInvariant();

                    if (_manifests.ContainsKey(name))
                    {
                        _installedNames.Add(name);
                    }
                }
            }

            lock (_enabledLock)
            {
                _enabledNames.Clear();

                foreach (var setting in settings)
                {
                    if (!setting.Type.StartsWith(FeaturePluginConstants.SettingPrefix))
                        continue;

                    if (!setting.Type.EndsWith(FeaturePluginConstants.EnabledSuffix))
                        continue;

                    if (!string.Equals(setting.Value, "true", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var name = setting.Type[FeaturePluginConstants.SettingPrefix.Length..^FeaturePluginConstants.EnabledSuffix.Length].ToLowerInvariant();

                    if (_manifests.ContainsKey(name))
                    {
                        _enabledNames.Add(name);
                    }
                }
            }

            _logger.LogInformation("Loaded {InstalledCount} installed and {EnabledCount} enabled feature plugin(s) from database",
                _installedNames.Count, _enabledNames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load installed feature plugins from database");
        }
    }
}
