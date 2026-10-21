// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raises every leg of the autofill duration matrix to the configured minimum travel time of the transport mode
/// (ROUTE_MIN_TRAVEL_TIME_BY_*, minutes). Diagonal and zero legs stay; a missing or unparsable setting means no floor.
/// </summary>
/// <param name="settingsReader">Reads the minimum travel time setting of the transport mode</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed class ContainerAutofillTravelTimeFloor
{
    private const double SecondsPerMinute = 60;

    private readonly ISettingsReader _settingsReader;

    public ContainerAutofillTravelTimeFloor(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task ApplyAsync(double[,] durationMatrix, ContainerTransportMode transportMode)
    {
        var minTravelTimeSeconds = await GetMinTravelTimeSecondsAsync(transportMode);
        ClampDurationMatrix(durationMatrix, minTravelTimeSeconds);
    }

    private static void ClampDurationMatrix(double[,] durationMatrix, double minTravelTimeSeconds)
    {
        var rows = durationMatrix.GetLength(0);
        var cols = durationMatrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (i != j && durationMatrix[i, j] > 0 && durationMatrix[i, j] < minTravelTimeSeconds)
                {
                    durationMatrix[i, j] = minTravelTimeSeconds;
                }
            }
        }
    }

    private async Task<double> GetMinTravelTimeSecondsAsync(ContainerTransportMode transportMode)
    {
        var settingKey = transportMode switch
        {
            ContainerTransportMode.ByBicycle => SettingKeys.RouteMinTravelTimeByBicycle,
            ContainerTransportMode.ByFoot => SettingKeys.RouteMinTravelTimeByFoot,
            _ => SettingKeys.RouteMinTravelTimeByCar
        };

        var setting = await _settingsReader.GetSetting(settingKey);
        if (setting != null && double.TryParse(setting.Value, out var minutes))
        {
            return minutes * SecondsPerMinute;
        }

        return 0;
    }
}
