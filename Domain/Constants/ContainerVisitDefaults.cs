// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fixed values of the container visit schedule, taken one to one from the Ui container template page
/// (ContainerTemplateItemManipulationService, ContainerTemplateRouteService) so both compute the same visit times.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerVisitDefaults
{
    public const double RoundingMidpoint = 0.5;

    public const int MinTravelMinutesByCar = 5;

    public const int MinTravelMinutesByBicycle = 3;

    public const int MinTravelMinutesByFoot = 1;

    public const int MovableBlockDefaultStartMinutes = 0;

    public const int MovableBlockDefaultEndMinutes = 30;

    public const int MinBlockDurationMinutes = 1;

    public const char SignMinus = '-';

    public const char SignPlus = '+';

    public const string TimeSpanWireFormat = "c";

    public const char TimeSpanPartSeparator = ':';

    public const int TimeSpanHoursPart = 0;

    public const int TimeSpanMinutesPart = 1;

    public const int TimeSpanSecondsPart = 2;

    public const string RoutingProfileDriving = "driving";

    public const string RoutingProfileCycling = "cycling";

    public const string RoutingProfileFoot = "foot";
}
