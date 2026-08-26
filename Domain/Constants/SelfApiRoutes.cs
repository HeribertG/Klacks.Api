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

    /// <summary>
    /// Named explicitly because GroupResource is served by two controllers (groups and group
    /// visibilities), so ISelfApiRouteResolver refuses to derive it from the type — see its
    /// TryResolve. Deriving it silently would send group writes to the visibility endpoint.
    /// </summary>
    public const string Groups = BackendPrefix + "Groups";

    /// <summary>
    /// Named explicitly because ContainerLocksController is not a generic CRUD controller, so
    /// ISelfApiRouteResolver — which reflects only over InputBaseController&lt;T&gt; — cannot derive it
    /// from a resource type at all.
    /// </summary>
    public const string ContainerLocks = BackendPrefix + "ContainerLocks";

    /// <summary>
    /// Named explicitly for the same reason as ContainerLocks: DonationController is not a generic CRUD
    /// controller, so ISelfApiRouteResolver — which reflects only over InputBaseController&lt;T&gt; —
    /// cannot derive it from a resource type at all. The full route is kept here rather than assembled
    /// at the call site so the single action segment cannot drift from the controller.
    /// </summary>
    public const string DonationCheckoutSession = BackendPrefix + "Donation/checkout-session";
}
