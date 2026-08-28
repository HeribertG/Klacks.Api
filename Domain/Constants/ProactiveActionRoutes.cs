// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Frontend routes proactive trigger events may offer as one-click action target. Values are taken
/// verbatim from Klacks.Api/Application/Skills/Definitions/navigation-targets.json (the single
/// source of truth for frontend routes) so the assistant never invents a path.
/// </summary>
public static class ProactiveActionRoutes
{
    public const string Schedule = "/workplace/schedule";
    public const string ClientEdit = "/workplace/edit-address";
    public const string ClientAvailability = "/workplace/client-availability";
    public const string PeriodClosing = "/workplace/period-closing";
    public const string Settings = "/workplace/settings";

    /// <summary>
    /// Value for ProactiveActionParamKeys.Target that opens the "Klacksy learns" card on the settings
    /// page. Matches the targetId in navigation-targets.json and the frontend's section mapping.
    /// </summary>
    public const string SettingsTargetKlacksyLearning = "assistant-learning";
}
