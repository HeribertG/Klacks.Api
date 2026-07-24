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
}
