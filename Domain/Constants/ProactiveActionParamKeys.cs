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
}
