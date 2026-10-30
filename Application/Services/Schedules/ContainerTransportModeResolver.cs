// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the transport mode text of the skills that build containers (car, bicycle, foot, mix) and tells whether it can be
/// used: every mode but car needs OpenRouteService, which the operator may have switched off and which needs an API key. A
/// refusal is returned as text before anything is written, naming the mode that works instead.
/// </summary>
/// <param name="geoServicesPolicy">Tells whether the operator switched OpenRouteService off</param>
/// <param name="settingsReader">Reads the configured OpenRouteService API key</param>
/// <param name="encryptionService">Decrypts the stored API key</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Extensions;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerTransportModeResolver
{
    public const string Car = "car";
    public const string Bicycle = "bicycle";
    public const string Foot = "foot";
    public const string Mix = "mix";

    private readonly IGeoServicesPolicy _geoServicesPolicy;
    private readonly ISettingsReader _settingsReader;
    private readonly ISettingsEncryptionService _encryptionService;

    public ContainerTransportModeResolver(
        IGeoServicesPolicy geoServicesPolicy,
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService)
    {
        _geoServicesPolicy = geoServicesPolicy;
        _settingsReader = settingsReader;
        _encryptionService = encryptionService;
    }

    /// <param name="value">Transport mode as the model passed it (case-insensitive)</param>
    public static (ContainerTransportMode? Mode, string? Error) Parse(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            Car => (ContainerTransportMode.ByCar, null),
            Bicycle => (ContainerTransportMode.ByBicycle, null),
            Foot => (ContainerTransportMode.ByFoot, null),
            Mix => (ContainerTransportMode.Mix, null),
            _ => (null, $"Invalid transportMode '{value}'. Expected one of: {Car}, {Bicycle}, {Foot}, {Mix}.")
        };
    }

    /// <param name="transportMode">The parsed mode</param>
    /// <param name="transportModeText">The mode as the model passed it, named in the refusal</param>
    /// <returns>The refusal text, or null when the mode can be used</returns>
    public async Task<string?> CheckAvailableAsync(ContainerTransportMode transportMode, string? transportModeText)
    {
        if (transportMode == ContainerTransportMode.ByCar)
        {
            return null;
        }

        if (!_geoServicesPolicy.OpenRouteService.IsEnabled)
        {
            return
                $"Cannot use transportMode '{transportModeText}' — {GeoServiceMessages.OpenRouteServiceDisabled}, so only " +
                "transportMode=car is available. Call this skill again with transportMode=car.";
        }

        var apiKey = await _settingsReader.GetOpenRouteServiceApiKeyAsync(_encryptionService);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return
                $"Cannot use transportMode '{transportModeText}' — no OpenRouteService API key is configured, so only " +
                "transportMode=car (driving times via OSRM/estimation) is available. Configure the OpenRouteService " +
                "API key in Settings first, or call this skill again with transportMode=car.";
        }

        return null;
    }
}
