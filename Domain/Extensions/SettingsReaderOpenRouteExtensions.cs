// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared lookup for the decrypted OpenRouteService API key setting. Centralizes the read-and-decrypt
/// pattern that used to be duplicated in DistanceMatrixBuilder and TravelTimeCalculationService, and is
/// also used by BundleNearbyTimeRangeShiftsIntoContainerSkill for its ORS-key gate.
/// </summary>
/// <param name="settingsReader">Read-only settings access (or any repository extending it).</param>
/// <param name="encryptionService">Decrypts the stored setting value.</param>

using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Domain.Extensions;

public static class SettingsReaderOpenRouteExtensions
{
    public static async Task<string?> GetOpenRouteServiceApiKeyAsync(
        this ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService)
    {
        var setting = await settingsReader.GetSetting(Application.Constants.Settings.OPENROUTESERVICE_API_KEY);
        if (setting == null || string.IsNullOrEmpty(setting.Value))
        {
            return null;
        }

        return encryptionService.ProcessForReading(setting.Type, setting.Value);
    }
}
