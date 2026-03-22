// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Narrow interface for read-only settings access in domain services.
/// </summary>
/// <param name="type">The settings key</param>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingsReader
{
    Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSetting(string type);
}
