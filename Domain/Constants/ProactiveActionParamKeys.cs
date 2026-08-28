// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Canonical parameter names for proactive one-click action routes, shared by all trigger events
/// so the frontend can rely on stable keys when preselecting the navigation context.
/// </summary>
public static class ProactiveActionParamKeys
{
    public const string GroupId = "groupId";
    public const string ClientId = "clientId";
    public const string ScenarioId = "scenarioId";
    public const string Date = "date";
    public const string Period = "period";

    /// <summary>
    /// Settings card the deep link should open, matching a targetId in navigation-targets.json. The
    /// settings page is a single route, so the card is addressed by this parameter rather than by a path.
    /// </summary>
    public const string Target = "target";
}
