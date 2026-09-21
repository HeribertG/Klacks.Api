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

    /// <summary>
    /// The employee list, for a finding that concerns several people at once and therefore has no
    /// single id to hand to ClientEdit above (whose component opens an empty new-employee form when
    /// it receives none).
    /// </summary>
    public const string ClientList = "/workplace/client";
    public const string ClientAvailability = "/workplace/client-availability";

    /// <summary>
    /// The group list, for a finding about the ABSENCE of groups: there is no single group to open,
    /// and the list is where the first one is created. Matches the group-list target in
    /// navigation-targets.json, whose required permission is CanViewGroups.
    /// </summary>
    public const string GroupList = "/workplace/group";

    /// <summary>
    /// The shift list, for a finding about shifts that carry no group membership: several shifts are
    /// concerned at once, so there is no single one to open. Matches the shift-list target in
    /// navigation-targets.json.
    /// </summary>
    public const string ShiftList = "/workplace/shift";
    public const string PeriodClosing = "/workplace/period-closing";
    public const string Settings = "/workplace/settings";

    /// <summary>
    /// Value for ProactiveActionParamKeys.Target that opens the "Klacksy learns" card on the settings
    /// page. Matches the targetId in navigation-targets.json and the frontend's section mapping.
    /// </summary>
    public const string SettingsTargetKlacksyLearning = "assistant-learning";
}
