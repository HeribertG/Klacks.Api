// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates a setting's value against key-specific rules before it is persisted through the
/// generic settings key/value store (GeneralSettingsController -> Post/PutCommandHandler). Keys
/// without a registered rule pass through unvalidated.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ISettingValueValidator
{
    void Validate(string key, string value);
}
