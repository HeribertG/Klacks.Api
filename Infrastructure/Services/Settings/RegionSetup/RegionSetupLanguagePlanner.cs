// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Plans and performs the languages section of a region-setup profile: validates languages.install
/// against the discovered language plugins (core languages are skipped), validates languages.default and
/// installs the planned plugins.
/// </summary>
/// <param name="languagePluginService">Discovers and installs language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionSetupLanguagePlanner
{
    private readonly ILanguagePluginService _languagePluginService;
    private readonly ILogger<RegionSetupLanguagePlanner> _logger;

    public RegionSetupLanguagePlanner(ILanguagePluginService languagePluginService, ILogger<RegionSetupLanguagePlanner> logger)
    {
        _languagePluginService = languagePluginService;
        _logger = logger;
    }

    public List<string> ValidateLanguages(RegionSetupLanguages? languages)
    {
        var toInstall = new List<string>();
        if (languages?.Install == null)
        {
            return toInstall;
        }

        foreach (var raw in languages.Install)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidRequestException("Region setup: languages.install contains an empty language code.");
            }

            var code = NormalizeLanguageCode(raw);
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
            {
                _logger.LogInformation("Region setup: language '{Code}' is a core language, skipping installation", code);
                continue;
            }

            if (_languagePluginService.GetPlugin(code) == null)
            {
                throw new InvalidRequestException($"Region setup: unknown language plugin code '{code}'. No plugin with this code was discovered.");
            }

            toInstall.Add(code);
        }

        return toInstall;
    }

    public void AddDefaultLanguageSetting(
        RegionSetupLanguages? languages,
        IReadOnlyCollection<string> languagesToInstall,
        List<(string Type, string Value)> settings)
    {
        if (string.IsNullOrWhiteSpace(languages?.Default))
        {
            return;
        }

        var code = NormalizeLanguageCode(languages.Default);
        var isValid = LanguagePluginConstants.CoreLanguages.Contains(code)
            || languagesToInstall.Contains(code)
            || _languagePluginService.GetPlugin(code) != null;

        if (!isValid)
        {
            throw new InvalidRequestException(
                $"Region setup: languages.default '{code}' is neither a core language, nor listed in languages.install, nor a discovered language plugin.");
        }

        settings.Add((SettingKeys.DefaultLanguage, code));
    }

    public async Task InstallLanguagesAsync(IReadOnlyList<string> codes)
    {
        foreach (var code in codes)
        {
            var installed = await _languagePluginService.InstallAsync(code);
            if (!installed)
            {
                throw new InvalidRequestException($"Region setup: installation of language plugin '{code}' failed.");
            }
        }
    }

    // Resolves a raw language code to its canonical form: core languages stay lowercase, while a
    // discovered plugin keeps the exact case of its manifest code (e.g. "zh-CN", "zh-TW"), which a
    // blanket ToLowerInvariant would break for the two mixed-case Chinese plugins.
    private string NormalizeLanguageCode(string raw)
    {
        var trimmed = raw.Trim();
        var lower = trimmed.ToLowerInvariant();
        if (LanguagePluginConstants.CoreLanguages.Contains(lower))
        {
            return lower;
        }

        return _languagePluginService.GetPlugin(trimmed)?.Code ?? lower;
    }
}
