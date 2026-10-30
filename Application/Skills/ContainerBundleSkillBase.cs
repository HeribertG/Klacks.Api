// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What the bundling skill and its preview share so they read the same input the same way: the parameters of a bundling and
/// the transport mode check, both through the parsers build_weekly_tours uses (ContainerSkillInputParser,
/// ContainerTransportModeResolver).
/// </summary>
/// <param name="settingsReader">Reads the OpenRouteService API key</param>
/// <param name="encryptionService">Decrypts the API key</param>
/// <param name="geoServicesPolicy">Tells whether OpenRouteService is switched on</param>

using System.Globalization;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.GeoServices;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public abstract class ContainerBundleSkillBase : BaseSkillImplementation
{
    protected ContainerBundleSkillBase(
        ISettingsReader settingsReader,
        ISettingsEncryptionService encryptionService,
        IGeoServicesPolicy geoServicesPolicy)
    {
        TransportModes = new ContainerTransportModeResolver(geoServicesPolicy, settingsReader, encryptionService);
    }

    protected ContainerTransportModeResolver TransportModes { get; }

    /// <param name="parameters">Raw skill parameters</param>
    /// <param name="language">Language the calendar strings are read in</param>
    /// <param name="defaultFromDate">Used when fromDate is missing; null makes fromDate required</param>
    protected static (ContainerBundleRequest? Request, string? Error) ParseBundleRequest(
        Dictionary<string, object> parameters, string? language, DateOnly? defaultFromDate = null)
    {
        var location = GetRequiredString(parameters, "location");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var fromTimeStr = GetRequiredString(parameters, "fromTime");
        var untilTimeStr = GetRequiredString(parameters, "untilTime");
        var fromDateStr = defaultFromDate is { } fallback
            ? GetParameter<string>(parameters, "fromDate") ?? fallback.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)
            : GetRequiredString(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var transportModeStr = GetParameter<string>(parameters, "transportMode") ?? ContainerTransportModeResolver.Car;
        var timeRangeTolerance = GetParameter<double>(parameters, "timeRangeTolerance", ContainerAutofillDefaults.RequestTimeRangeTolerance);
        var groupIdsRaw = GetParameter<string>(parameters, "groupIds");
        var groupName = GetParameter<string>(parameters, "groupName");
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"Mobile Dienste {location}";
        }

        if (!IsoWeekday.IsValid(isoWeekday))
        {
            return (null, IsoWeekday.RangeError);
        }

        var (fromTime, untilTime, timeError) = ContainerSkillInputParser.ParseTimeBudget(fromTimeStr, untilTimeStr, language);
        if (timeError != null)
        {
            return (null, timeError);
        }

        var (fromDate, untilDate, dateError) = ContainerSkillInputParser.ParseValidity(fromDateStr, untilDateStr, language);
        if (dateError != null)
        {
            return (null, dateError);
        }

        var (transportMode, transportModeError) = ContainerTransportModeResolver.Parse(transportModeStr);
        if (transportMode is null)
        {
            return (null, transportModeError!);
        }

        var storageWeekday = IsoWeekday.ToStorage(isoWeekday);

        return (new ContainerBundleRequest(
            location, isoWeekday, storageWeekday, fromTime, untilTime, fromDate, untilDate,
            transportModeStr, transportMode.Value, timeRangeTolerance, groupIdsRaw, groupName, name), null);
    }
}
