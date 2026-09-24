// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads DEFAULT_LANGUAGE through the settings reader. Deliberately does not filter the value against
/// LanguageConfig.SupportedLanguages: that list is never fed with the installed language packs at runtime, so
/// a filter would turn every pack language into English. A blank or unreadable setting falls back to
/// LanguageConfig.DefaultLanguageFallback and is logged, never thrown, because a text in the wrong language
/// is better than a planner notice that is not written at all. Scoped, like the settings reader it uses.
/// </summary>
/// <param name="settingsReader">Reads the DEFAULT_LANGUAGE setting</param>
/// <param name="logger">Logs an unreadable setting</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public sealed class InstallationLanguageResolver : IInstallationLanguageResolver
{
    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<InstallationLanguageResolver> _logger;

    public InstallationLanguageResolver(ISettingsReader settingsReader, ILogger<InstallationLanguageResolver> logger)
    {
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<string> ResolveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configured = (await _settingsReader.GetSetting(SettingKeys.DefaultLanguage))?.Value;
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Could not read the installation language; texts go out in {Language}", LanguageConfig.DefaultLanguageFallback);
        }

        return LanguageConfig.DefaultLanguageFallback;
    }
}
