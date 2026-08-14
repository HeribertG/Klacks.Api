// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingsSecretResolver
{
    Task<string> ResolveAsync(string settingType, string? providedValue);

    Task<string> ResolveBoundAsync(string settingType, string? providedValue, params SecretBinding[] bindings);
}
