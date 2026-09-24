// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the language of the installation for texts that are written by the server without a browser
/// session behind them (planner notices, mails to employees). The server knows one language per
/// installation, DEFAULT_LANGUAGE, not one per user. The value is returned as configured, possibly
/// regional such as "de-CH" or "zh-CN"; whether a text exists for it is the catalogue's decision, and only
/// a missing or unreadable setting yields the fallback language.
/// </summary>
/// <param name="cancellationToken">Cancels the settings read</param>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IInstallationLanguageResolver
{
    Task<string> ResolveAsync(CancellationToken cancellationToken = default);
}
