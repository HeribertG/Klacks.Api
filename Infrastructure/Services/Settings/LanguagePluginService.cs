// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade for language plugin management: discovery, installation, uninstallation and translations.
/// Delegates geo data operations to <see cref="LanguagePluginGeoDataInstaller"/>,
/// content operations to <see cref="LanguagePluginContentInstaller"/>
/// and skill label operations to <see cref="LanguagePluginSkillLabelInstaller"/>.
/// </summary>
/// <param name="scopeFactory">Factory for DI scopes in database operations</param>
/// <param name="configuration">App configuration for the plugin directory</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Services.Assistant;
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
    private readonly ConcurrentDictionary<string, Dictionary<string, string>> _translationCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _installedCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _installedLock = new();
    private bool _initialized;

    private readonly LanguagePluginGeoDataInstaller _geoDataInstaller;
    private readonly LanguagePluginContentInstaller _contentInstaller;
    private readonly LanguagePluginSkillLabelInstaller _skillLabelInstaller;

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
        _skillLabelInstaller = new LanguagePluginSkillLabelInstaller(_pluginDirectory, _logger);
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
        await RunForEachInstalledCodeAsync(
            _contentInstaller.MergeDefaultGeoTranslationsAsync,
            "Failed to backfill default geo translations for installed language plugins");
    }

    /// <summary>
    /// Writes the recipe veto vocabulary of every installed pack into the enabled recipes on startup.
    /// Without this the column stays empty on every existing installation: a pack only reaches the
    /// recipes that are enabled at the moment it is installed, so a column added later would need all
    /// 21 packs uninstalled and reinstalled by hand before a single plugin-language question was vetoed.
    /// Deliberately NOT called from InitializeAsync. That runs inside the same Task.WhenAll as
    /// LoadRecipeSeedsAsync in Program.cs, so a backfill there would race the seeder and silently skip
    /// every recipe row that did not exist yet - the same unordered-execution failure the skill-seed
    /// chaining in that batch documents. Program.cs therefore calls this only after the batch completed.
    /// </summary>
    public async Task ApplyInstalledRecipeVetoesAsync()
    {
        await InitializeAsync();

        await RunForEachInstalledCodeAsync(
            _contentInstaller.InstallRecipeVetoesAsync,
            "Failed to backfill recipe vetoes for installed language plugins");
    }

    /// <summary>
    /// Writes the labels of every installed pack into the enabled skills on startup. Without this the
    /// column stays empty on every existing installation for exactly the reason
    /// ApplyInstalledRecipeVetoesAsync gives: a pack only reaches the skills that exist at the moment it
    /// is installed, so a column added later would need all 21 packs uninstalled and reinstalled by hand.
    /// Deliberately NOT called from InitializeAsync - it depends on the skill rows the chained
    /// InitializeFeaturePluginsThenLoadSkillSeedsAsync branch creates, which is a parallel branch of the
    /// same Task.WhenAll, so Program.cs calls this only after that batch completed.
    /// </summary>
    public async Task ApplyInstalledSkillLabelsAsync()
    {
        await InitializeAsync();

        await RunForEachInstalledCodeAsync(
            (scope, code) => _skillLabelInstaller.InstallSkillLabelsAsync(scope, code),
            "Failed to backfill skill labels for installed language plugins");
    }

    /// <summary>
    /// Writes the skill synonyms of every installed pack into the enabled skills that have none of that
    /// language yet. A pack only reaches the skills that exist at the moment it is installed, so a skill
    /// seeded later - or a skill added to a pack file later - otherwise kept only its core-language
    /// synonyms until the pack was reinstalled by hand. Skills that already carry the language are not
    /// written, so a normal boot costs one read per pack. Same ordering constraint as
    /// ApplyInstalledSkillLabelsAsync, and it has to finish before the knowledge index sync at host start
    /// so the new phrases get embedded.
    /// </summary>
    public async Task ApplyInstalledSkillSynonymBackfillAsync()
    {
        await InitializeAsync();

        await RunForEachInstalledCodeAsync(
            _contentInstaller.BackfillMissingSkillSynonymsAsync,
            "Failed to backfill skill synonyms for installed language plugins");
    }

    /// <summary>
    /// Maps any spelling of a pack code (zh-cn, ZH-CN) to the one its manifest declares (zh-CN). Every
    /// installed code is held in that spelling: it keys synonyms, labels, skill_phrase rows and docs, and
    /// it names the pack directory, which a case-sensitive file system only finds under that spelling.
    /// A code without a discovered manifest is returned unchanged.
    /// </summary>
    private string ToManifestCode(string code) =>
        _manifests.TryGetValue(code, out var manifest) ? manifest.Code : code;

    /// <summary>
    /// Re-syncs manual docs from the plugin directory into the database for every already-installed
    /// language on each startup, so manuals added to a plugin after its initial install are picked up
    /// without requiring an uninstall/reinstall cycle.
    /// </summary>
    private async Task BackfillDocsAsync()
    {
        await RunForEachInstalledCodeAsync(
            _contentInstaller.InstallDocsAsync,
            "Failed to backfill docs for installed language plugins",
            afterAll: async scope =>
            {
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                await unitOfWork.CompleteAsync();
            });
    }

    /// <summary>
    /// Re-runs the plugin country upsert for every already-installed language on each startup, so a
    /// plugin's home country is picked up even if it was added to countries.json after the plugin
    /// was originally installed.
    /// </summary>
    private async Task BackfillCountriesAsync()
    {
        await RunForEachInstalledCodeAsync(
            async (scope, code) =>
            {
                await _contentInstaller.InstallCountryAsync(scope, code);
                await _contentInstaller.InstallStatesAsync(scope, code);
            },
            "Failed to backfill countries for installed language plugins");
    }

    /// <summary>
    /// Shared skeleton behind every "reapply this per installed code" backfill above: snapshot the
    /// installed codes under lock, return without opening a scope when there are none, then run
    /// <paramref name="install"/> once per code inside a single shared scope, as BackfillCountriesAsync
    /// needs for its InstallCountryAsync/InstallStatesAsync pair. <paramref name="afterAll"/> exists only
    /// for BackfillDocsAsync, whose installer stages entities on the plain DataBaseContext without
    /// committing; every other installer here commits itself per call, so afterAll stays null for them.
    /// A failure anywhere logs <paramref name="failureMessage"/> and swallows, matching each installer
    /// method's own catch/log-and-continue behavior: one broken pack must not stop the others.
    /// </summary>
    private async Task RunForEachInstalledCodeAsync(
        Func<IServiceScope, string, Task> install,
        string failureMessage,
        Func<IServiceScope, Task>? afterAll = null)
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
                await install(scope, code);
            }

            if (afterAll != null)
            {
                await afterAll(scope);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, failureMessage);
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

        code = manifest.Code;

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
        await _skillLabelInstaller.InstallSkillLabelsAsync(scope, code);
        await _contentInstaller.InstallRecipeSynonymsAsync(scope, code);
        await _contentInstaller.InstallRecipeVetoesAsync(scope, code);
        await _contentInstaller.InstallNavigationSynonymsAsync(scope, code);
        await _contentInstaller.InstallSentimentKeywordsAsync(scope, code);
        await _contentInstaller.InstallWakeWordsAsync(code);
        await unitOfWork.CompleteAsync();
        await _contentInstaller.MergeNonCoreTranslationsAsync(scope, code);
        await _contentInstaller.MergeDefaultGeoTranslationsAsync(scope, code);
        await _contentInstaller.InstallCountryAsync(scope, code);
        await _contentInstaller.InstallStatesAsync(scope, code);

        // The pack just changed skill and recipe synonyms; without this refresh the retrieval index
        // keeps matching on the pre-install keywords until the next application start. The index sync
        // only gets scheduled here: a new language re-embeds several hundred entries (~20 min), far
        // beyond the proxy timeout, so semantic retrieval catches up in the background.
        await scope.ServiceProvider.GetRequiredService<ISkillCatalogRefresher>()
            .RefreshAsync($"installing language plugin '{code}'");

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

        code = ToManifestCode(code);
        var settingKey = LanguagePluginConstants.SettingPrefix + code.ToUpperInvariant();

        using var scope = _scopeFactory.CreateScope();
        var settingsRepo = scope.ServiceProvider.GetRequiredService<ISettingsRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await _contentInstaller.UninstallSkillSynonymsAsync(scope, code);
        await _skillLabelInstaller.UninstallSkillLabelsAsync(scope, code);
        await _contentInstaller.UninstallRecipeSynonymsAsync(scope, code);
        await _contentInstaller.UninstallRecipeVetoesAsync(scope, code);
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

        await scope.ServiceProvider.GetRequiredService<ISkillCatalogRefresher>()
            .RefreshAsync($"uninstalling language plugin '{code}'");

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

        code = ToManifestCode(code);

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

        var pluginCode = ToManifestCode(code);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var doc = await db.PluginDocs
            .FirstOrDefaultAsync(d => d.PluginCode == pluginCode && d.ManualName == manualName);

        return doc?.HtmlContent;
    }

    public async Task RefreshPluginsAsync()
    {
        _manifests.Clear();
        _translationCache.Clear();
        _initialized = false;
        await InitializeAsync();
    }

    public async Task ApplyInstalledSkillSynonymsAsync(IReadOnlyCollection<string> skillNames)
    {
        if (skillNames.Count == 0)
            return;

        await InitializeAsync();

        var codes = GetInstalledPluginCodes();
        if (codes.Count == 0)
            return;

        using var scope = _scopeFactory.CreateScope();

        foreach (var code in codes)
        {
            await _contentInstaller.InstallSkillSynonymsAsync(scope, code, skillNames);
            await _skillLabelInstaller.InstallSkillLabelsAsync(scope, code, skillNames);
        }
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

                    var settingCode = setting.Type[LanguagePluginConstants.SettingPrefix.Length..];

                    if (_manifests.TryGetValue(settingCode, out var manifest))
                    {
                        _installedCodes.Add(manifest.Code);
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
