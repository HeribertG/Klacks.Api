// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Schmales Interface fuer lesenden Settings-Zugriff in Domain-Services.
/// </summary>
/// <param name="type">Der Settings-Schluessel</param>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingsReader
{
    Task<Klacks.Api.Domain.Models.Settings.Settings?> GetSetting(string type);
}
