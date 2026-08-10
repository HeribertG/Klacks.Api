// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingsSecretResolver
{
    Task<string> ResolveAsync(string settingType, string? providedValue);
}
