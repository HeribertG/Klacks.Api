// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Narrow interface for read-only settings access in domain services.
/// </summary>
/// <param name="type">The settings key</param>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingsReader
{
    Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSetting(string type);

    Task<IReadOnlyDictionary<string, string>> GetSettingsByTypesAsync(IEnumerable<string> types, CancellationToken cancellationToken = default);
}
