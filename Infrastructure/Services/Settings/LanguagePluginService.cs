// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Config;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
                MinKlacksVersion = manifest.MinKlacksVersion,
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
            MinKlacksVersion = manifest.MinKlacksVersion,
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
                code, manifest.MinKlacksVersion, $"{MyVersion.Major}.{MyVersion.Minor}.{MyVersion.Patch}");
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

        await InstallGeoDataAsync(scope, code);
        await InstallDocsAsync(scope, code);
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

        await UninstallGeoDataAsync(scope, code);
        await UninstallDocsAsync(scope, code);

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

    private Dictionary<string, string> LoadStateNamesForLanguage(string code, string languageCode)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var filePath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.StatesFileName);

        if (!File.Exists(filePath))
            return result;

        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var abbreviation = element.GetProperty("abbreviation").GetString();
                if (string.IsNullOrEmpty(abbreviation))
                    continue;

                if (element.TryGetProperty("name", out var nameObj)
                    && nameObj.TryGetProperty(languageCode, out var langValue))
                {
                    var name = langValue.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        result[abbreviation] = name;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load state names for language '{Code}' in plugin '{PluginCode}'",
                languageCode, code);
        }

        return result;
    }

    private List<T>? LoadPluginDataFile<T>(string code, string fileName)
    {
        var filePath = Path.Combine(_pluginDirectory, code, fileName);
        if (!File.Exists(filePath))
            return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin data file '{FileName}' for language '{Code}'", fileName, code);
            return null;
        }
    }

    private async Task InstallGeoDataAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var countries = LoadPluginDataFile<Countries>(code, LanguagePluginConstants.CountriesFileName);
        if (countries != null)
        {
            foreach (var country in countries)
            {
                var existing = await db.Countries.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == country.Id);

                if (existing != null)
                {
                    existing.IsDeleted = false;
                    existing.DeletedTime = null;
                }
                else
                {
                    db.Countries.Add(country);
                }
            }
        }

        var states = LoadPluginDataFile<State>(code, LanguagePluginConstants.StatesFileName);
        if (states != null)
        {
            foreach (var state in states)
            {
                var existing = await db.State.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.Id == state.Id);

                if (existing != null)
                {
                    existing.IsDeleted = false;
                    existing.DeletedTime = null;
                }
                else
                {
                    db.State.Add(state);
                }
            }
        }

        var rules = LoadPluginDataFile<CalendarRule>(code, LanguagePluginConstants.CalendarRulesFileName);
        if (rules != null)
        {
            foreach (var rule in rules)
            {
                if (!await db.CalendarRule.AnyAsync(r => r.Id == rule.Id))
                {
                    db.CalendarRule.Add(rule);
                }
            }
        }

        _logger.LogInformation(
            "Installed geo data for language plugin '{Code}': {Countries} countries, {States} states, {Rules} calendar rules",
            code, countries?.Count ?? 0, states?.Count ?? 0, rules?.Count ?? 0);

        if (rules is { Count: > 0 } && states is { Count: > 0 } && countries is { Count: > 0 })
        {
            await InstallCalendarSelectionsAsync(db, code, countries, states, rules);
        }
    }

    private async Task InstallCalendarSelectionsAsync(
        DataBaseContext db,
        string code,
        List<Countries> countries,
        List<State> states,
        List<CalendarRule> rules)
    {
        var manifest = _manifests.GetValueOrDefault(code);
        var languageCode = manifest?.Code ?? "en";

        var stateNameOverrides = LoadStateNamesForLanguage(code, languageCode);

        var regionalStateCodes = rules
            .Where(r => r.State != r.Country)
            .Select(r => r.State)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var countryAbbreviations = countries
            .Select(c => c.Abbreviation)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = 0;

        foreach (var state in states)
        {
            if (!countryAbbreviations.Contains(state.CountryPrefix))
                continue;

            var stateName = stateNameOverrides.GetValueOrDefault(state.Abbreviation)
                ?? state.Name.GetValue(languageCode)
                ?? state.Name.En
                ?? state.Abbreviation;

            var existing = await db.CalendarSelection.IgnoreQueryFilters()
                .FirstOrDefaultAsync(cs => cs.PluginCode == code && cs.Name == stateName);

            if (existing != null)
            {
                existing.IsDeleted = false;
                existing.DeletedTime = null;
                continue;
            }

            var calendarSelection = new CalendarSelection
            {
                Id = Guid.NewGuid(),
                Name = stateName,
                PluginCode = code,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = "System",
                SelectedCalendars = new List<SelectedCalendar>()
            };

            calendarSelection.SelectedCalendars.Add(new SelectedCalendar
            {
                Id = Guid.NewGuid(),
                CalendarSelectionId = calendarSelection.Id,
                Country = state.CountryPrefix,
                State = state.CountryPrefix,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = "System"
            });

            if (regionalStateCodes.Contains(state.Abbreviation))
            {
                calendarSelection.SelectedCalendars.Add(new SelectedCalendar
                {
                    Id = Guid.NewGuid(),
                    CalendarSelectionId = calendarSelection.Id,
                    Country = state.CountryPrefix,
                    State = state.Abbreviation,
                    CreateTime = DateTime.UtcNow,
                    CurrentUserCreated = "System"
                });
            }

            db.CalendarSelection.Add(calendarSelection);
            count++;
        }

        _logger.LogInformation(
            "Installed {Count} calendar selection(s) for language plugin '{Code}'",
            count, code);
    }

    private async Task UninstallGeoDataAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var calendarSelections = await db.CalendarSelection
            .Include(cs => cs.SelectedCalendars)
            .Where(cs => cs.PluginCode == code)
            .ToListAsync();

        if (calendarSelections.Count > 0)
        {
            db.CalendarSelection.RemoveRange(calendarSelections);
            _logger.LogInformation(
                "Uninstalled {Count} calendar selection(s) for language plugin '{Code}'",
                calendarSelections.Count, code);
        }

        var countries = LoadPluginDataFile<Countries>(code, LanguagePluginConstants.CountriesFileName);
        if (countries == null || countries.Count == 0)
            return;

        var abbreviations = countries.Select(c => c.Abbreviation).ToList();

        var calendarRules = await db.CalendarRule
            .Where(r => abbreviations.Contains(r.Country))
            .ToListAsync();
        db.CalendarRule.RemoveRange(calendarRules);

        var statesToRemove = await db.State
            .Where(s => abbreviations.Contains(s.CountryPrefix))
            .ToListAsync();
        db.State.RemoveRange(statesToRemove);

        var countriesToRemove = await db.Countries
            .Where(c => abbreviations.Contains(c.Abbreviation))
            .ToListAsync();
        db.Countries.RemoveRange(countriesToRemove);

        _logger.LogInformation(
            "Uninstalled geo data for language plugin '{Code}': {Countries} countries, {States} states, {Rules} calendar rules",
            code, countriesToRemove.Count, statesToRemove.Count, calendarRules.Count);
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

    private async Task InstallDocsAsync(IServiceScope scope, string code)
    {
        var docsPath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.DocsDirectory);
        if (!Directory.Exists(docsPath))
            return;

        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var htmlFiles = Directory.GetFiles(docsPath, "*.html");
        var count = 0;

        foreach (var filePath in htmlFiles)
        {
            var manualName = Path.GetFileNameWithoutExtension(filePath);
            var htmlContent = await File.ReadAllTextAsync(filePath);

            var existing = await db.PluginDocs
                .FirstOrDefaultAsync(d => d.PluginCode == code && d.ManualName == manualName);

            if (existing != null)
            {
                existing.HtmlContent = htmlContent;
            }
            else
            {
                db.PluginDocs.Add(new PluginDoc
                {
                    Id = Guid.NewGuid(),
                    PluginCode = code,
                    ManualName = manualName,
                    HtmlContent = htmlContent
                });
            }

            count++;
        }

        _logger.LogInformation("Installed {Count} doc(s) for language plugin '{Code}'", count, code);
    }

    private async Task UninstallDocsAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var docs = await db.PluginDocs
            .Where(d => d.PluginCode == code)
            .ToListAsync();

        db.PluginDocs.RemoveRange(docs);
        _logger.LogInformation("Uninstalled {Count} doc(s) for language plugin '{Code}'", docs.Count, code);
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
