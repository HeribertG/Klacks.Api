// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Routes the assistant calls on the own API. They follow BaseController's "api/backend/[controller]"
/// convention, so the segment is the controller name without its suffix.
/// </summary>
public static class SelfApiRoutes
{
    private const string BackendPrefix = "api/backend/";

    public const string Expenses = BackendPrefix + "Expenses";
}
