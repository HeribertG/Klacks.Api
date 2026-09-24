// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads DEFAULT_LANGUAGE through the settings reader. Deliberately does not filter the value against
/// LanguageConfig.SupportedLanguages: that list is never fed with the installed language packs at runtime, so
/// a filter would turn every pack language into English. A blank or unreadable setting falls back to
/// LanguageConfig.DefaultLanguageFallback and is logged, never thrown, because a text in the wrong language
/// is better than a planner notice that is not written at all. A cancelled call is the one exception: it
/// rethrows, so a stopping caller is not held up and no wrong-language text is built for it. Scoped, like the
/// settings reader it uses.
/// </summary>
/// <param name="settingsReader">Reads the DEFAULT_LANGUAGE setting</param>
/// <param name="logger">Logs an unreadable setting</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Settings;

public sealed class InstallationLanguageResolver : IInstallationLanguageResolver
{
    private static readonly string[] DefaultLanguageKeys = [SettingKeys.DefaultLanguage];

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
            var settings = await _settingsReader.GetSettingsByTypesAsync(DefaultLanguageKeys, cancellationToken);
            if (settings.TryGetValue(SettingKeys.DefaultLanguage, out var configured) && !string.IsNullOrWhiteSpace(configured))
            {
                return configured.Trim();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "Could not read the installation language; texts go out in {Language}", LanguageConfig.DefaultLanguageFallback);
        }

        return LanguageConfig.DefaultLanguageFallback;
    }
}
