// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Singleton service for feature plugin lifecycle management: discovery, installation, enable/disable.
/// </summary>
/// <param name="scopeFactory">Factory for DI scopes in database operations</param>
/// <param name="configuration">App configuration for the plugin directory</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Plugins;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Plugins;
using Klacks.Api.Infrastructure.Persistence.Seed;
using Klacks.Plugin.Contracts;

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

    private const string LanguageCodePattern = "^[A-Za-z0-9_-]+$";

    private static readonly Regex LanguageCodeRegex = new(LanguageCodePattern, RegexOptions.Compiled);

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

    public async Task<IReadOnlyList<FeaturePluginInfo>> GetAllPluginsAsync()
    {
        var operationalChecks = await GetOperationalChecksAsync();
        var plugins = new List<FeaturePluginInfo>();

        foreach (var kvp in _manifests)
        {
            var manifest = kvp.Value;
            var isInstalled = IsInstalled(manifest.Name);
            var isEnabled = IsEnabled(manifest.Name);

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
                IsInstalled = isInstalled,
                IsEnabled = isEnabled,
                IsOperational = isInstalled && isEnabled
                    ? operationalChecks.GetValueOrDefault(manifest.Name, true)
                    : true,
                Navigation = manifest.Navigation
            });
        }

        return plugins;
    }

    public async Task<FeaturePluginInfo?> GetPluginAsync(string name)
    {
        if (!_manifests.TryGetValue(name, out var manifest))
            return null;

        var isInstalled = IsInstalled(name);
        var isEnabled = IsEnabled(name);
        var isOperational = true;

        if (isInstalled && isEnabled)
        {
            var operationalChecks = await GetOperationalChecksAsync();
            isOperational = operationalChecks.GetValueOrDefault(name, true);
        }

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
            IsInstalled = isInstalled,
            IsEnabled = isEnabled,
            IsOperational = isOperational,
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
                name.ForLog(), manifest.MinKlacksVersion, $"{MyVersion.Major}.{MyVersion.Minor}.{MyVersion.Patch}");
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

        await RegisterPluginNavigationAsync(scope, manifest);

        // The membership sets above are what the seed loader reads to decide which plugins to seed,
        // so this call has to come after them - and after CompleteAsync, or the catalogue refresh
        // that follows would rebuild the knowledge index from an uncommitted transaction.
        await SeedPluginSkillsAsync(scope, name, $"installing feature plugin '{name}'");

        _logger.LogInformation("Feature plugin '{Name}' installed", name.ForLog());
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

        if (_manifests.TryGetValue(name, out var uninstallManifest))
        {
            await UnregisterPluginNavigationAsync(scope, uninstallManifest);
        }

        // Disabled, never deleted: the rows follow the soft-delete convention and the plugin may be
        // installed again. Without this the skills stayed in the catalogue and in the knowledge index
        // until the next application start, so the assistant kept offering tools it could not run.
        await DisablePluginSkillsAsync(scope, name, $"uninstalling feature plugin '{name}'");

        _logger.LogInformation("Feature plugin '{Name}' uninstalled", name.ForLog());
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

        await SeedPluginSkillsAsync(scope, name, $"enabling feature plugin '{name}'");

        _logger.LogInformation("Feature plugin '{Name}' enabled", name.ForLog());
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

        await DisablePluginSkillsAsync(scope, name, $"disabling feature plugin '{name}'");

        _logger.LogInformation("Feature plugin '{Name}' disabled", name.ForLog());
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
        if (string.IsNullOrWhiteSpace(lang) || !LanguageCodeRegex.IsMatch(lang))
            return null;

        var safeLang = Path.GetFileName(lang);
        if (string.IsNullOrEmpty(safeLang) || !LanguageCodeRegex.IsMatch(safeLang))
            return null;

        var merged = new Dictionary<string, string>();

        foreach (var kvp in _manifests)
        {
            if (!IsInstalled(kvp.Key) || !IsEnabled(kvp.Key))
                continue;

            var pluginDir = Path.Combine(_pluginDirectory, kvp.Key, FeaturePluginConstants.I18nDirectory);
            var filePath = Path.Combine(pluginDir, $"{safeLang}.json");

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
                _logger.LogError(ex, "Failed to load translations for plugin '{Plugin}' lang '{Lang}'", kvp.Key, safeLang.ForLog());
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

    /// <summary>
    /// Seeds a plugin's skills and puts them into the catalogue and the knowledge index right away.
    /// Enabling is a separate step from seeding on purpose: the seed loader only writes a definition
    /// whose version is strictly greater than the stored one, so a plugin that is installed, removed
    /// and installed again would otherwise come back with its skills still disabled.
    /// </summary>
    /// <param name="scope">The scope whose database work has already been committed</param>
    /// <param name="name">Plugin name, used as the directory name under the plugin root</param>
    /// <param name="reason">What triggered the refresh, for the log line</param>
    private async Task SeedPluginSkillsAsync(IServiceScope scope, string name, string reason)
    {
        try
        {
            var seedLoader = scope.ServiceProvider.GetRequiredService<SkillSeedLoader>();
            await seedLoader.SeedPluginSkillsAsync(name);
            await seedLoader.SetPluginSkillsEnabledAsync(name, isEnabled: true);
            await RefreshSkillCatalogAsync(scope, reason);
        }
        catch (Exception ex)
        {
            // Never fails the lifecycle operation: the plugin itself is installed and working at this
            // point, and a restart re-runs the seed. Losing the assistant integration is a smaller
            // failure than reporting an install that did happen as failed.
            _logger.LogError(ex, "Failed to seed skills for feature plugin '{Name}'", name.ForLog());
        }
    }

    /// <summary>
    /// Takes a plugin's skills out of the assistant without deleting them, and rebuilds the catalogue
    /// so the change is visible before the skill cache expires.
    /// </summary>
    /// <param name="scope">The scope whose database work has already been committed</param>
    /// <param name="name">Plugin name, used as the directory name under the plugin root</param>
    /// <param name="reason">What triggered the refresh, for the log line</param>
    private async Task DisablePluginSkillsAsync(IServiceScope scope, string name, string reason)
    {
        try
        {
            var seedLoader = scope.ServiceProvider.GetRequiredService<SkillSeedLoader>();
            await seedLoader.SetPluginSkillsEnabledAsync(name, isEnabled: false);
            await RefreshSkillCatalogAsync(scope, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable skills for feature plugin '{Name}'", name.ForLog());
        }
    }

    private static async Task RefreshSkillCatalogAsync(IServiceScope scope, string reason) =>
        await scope.ServiceProvider.GetRequiredService<ISkillCatalogRefresher>().RefreshAsync(reason);

    /// <summary>
    /// Adds the plugin's route to the navigate_to skill. Called from InstallAsync only, which follows
    /// up with a catalogue refresh - that is what makes the changed route visible, since the skill
    /// cache would otherwise serve the old handler config for up to its five-minute lifetime. The
    /// knowledge index is not affected: routes live in HandlerConfig, which is not part of the
    /// embedded text.
    /// </summary>
    /// <param name="scope">Scope providing the skill and agent repositories</param>
    /// <param name="manifest">Manifest carrying the navigation route to register</param>
    private async Task RegisterPluginNavigationAsync(IServiceScope scope, FeaturePluginManifest manifest)
    {
        if (manifest.Navigation == null || string.IsNullOrEmpty(manifest.Navigation.Route))
            return;

        try
        {
            var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
            var agentRepo = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
            var agent = await agentRepo.GetDefaultAgentAsync();
            if (agent == null) return;

            var skill = await skillRepo.GetByNameAsync(agent.Id, "navigate_to");
            if (skill == null) return;

            var routes = ParseRoutes(skill.HandlerConfig);
            routes[manifest.Name] = manifest.Navigation.Route;
            skill.HandlerConfig = SerializeRoutes(routes);

            UpdateEnumValues(skill, routes.Keys.ToList());
            await skillRepo.UpdateAsync(skill);

            _logger.LogInformation("Registered navigation route '{Name}' -> '{Route}' for plugin '{Plugin}'",
                manifest.Name, manifest.Navigation.Route, manifest.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to register navigation for plugin '{Name}'", manifest.Name);
        }
    }

    private async Task UnregisterPluginNavigationAsync(IServiceScope scope, FeaturePluginManifest manifest)
    {
        if (manifest.Navigation == null || string.IsNullOrEmpty(manifest.Navigation.Route))
            return;

        try
        {
            var skillRepo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
            var agentRepo = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
            var agent = await agentRepo.GetDefaultAgentAsync();
            if (agent == null) return;

            var skill = await skillRepo.GetByNameAsync(agent.Id, "navigate_to");
            if (skill == null) return;

            var routes = ParseRoutes(skill.HandlerConfig);
            routes.Remove(manifest.Name);
            skill.HandlerConfig = SerializeRoutes(routes);

            UpdateEnumValues(skill, routes.Keys.ToList());
            await skillRepo.UpdateAsync(skill);

            _logger.LogInformation("Unregistered navigation route for plugin '{Name}'", manifest.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to unregister navigation for plugin '{Name}'", manifest.Name);
        }
    }

    private static Dictionary<string, string> ParseRoutes(string? handlerConfig)
    {
        if (string.IsNullOrEmpty(handlerConfig) || handlerConfig == "{}")
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var doc = JsonDocument.Parse(handlerConfig);
            if (doc.RootElement.TryGetProperty("routes", out var routesElement))
            {
                var routes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in routesElement.EnumerateObject())
                {
                    routes[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
                return routes;
            }
        }
        catch (JsonException) { }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    private static string SerializeRoutes(Dictionary<string, string> routes)
    {
        return JsonSerializer.Serialize(new { routes }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    private static void UpdateEnumValues(Domain.Models.Assistant.AgentSkill skill, List<string> pageKeys)
    {
        try
        {
            var parameters = JsonSerializer.Deserialize<List<JsonElement>>(skill.ParametersJson);
            if (parameters == null || parameters.Count == 0) return;

            var updatedParams = new List<Dictionary<string, object?>>();
            foreach (var param in parameters)
            {
                var dict = new Dictionary<string, object?>
                {
                    ["name"] = param.GetProperty("name").GetString(),
                    ["description"] = param.GetProperty("description").GetString(),
                    ["type"] = param.GetProperty("type").GetString(),
                    ["required"] = param.GetProperty("required").GetBoolean(),
                    ["defaultValue"] = param.TryGetProperty("defaultValue", out var dv) && dv.ValueKind != JsonValueKind.Null ? dv.GetString() : null,
                    ["enumValues"] = param.TryGetProperty("enumValues", out var ev) && ev.ValueKind != JsonValueKind.Null
                        ? ev.EnumerateArray().Select(e => e.GetString()).ToList()
                        : null
                };

                if (dict["name"]?.ToString() == "page")
                {
                    dict["enumValues"] = pageKeys;
                }

                updatedParams.Add(dict);
            }

            skill.ParametersJson = JsonSerializer.Serialize(updatedParams, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }
        catch (JsonException) { }
    }

    private async Task<Dictionary<string, bool>> GetOperationalChecksAsync()
    {
        var results = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var checks = scope.ServiceProvider.GetServices<IPluginOperationalCheck>();

            foreach (var check in checks)
            {
                try
                {
                    results[check.PluginName] = await check.IsOperationalAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Operational check failed for plugin '{Name}', assuming not operational", check.PluginName);
                    results[check.PluginName] = false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve plugin operational checks");
        }

        return results;
    }
}
