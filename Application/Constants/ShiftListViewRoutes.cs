// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// The shift list with a view preselected, mirroring Klacks.Ui shift-list-view.constants.ts: "view=orders" opens the
/// order intake (original orders not sealed yet), "view=plannable" the plannable shifts, optionally narrowed to the
/// period given by "from" and "until" (ISO dates). A skill puts such a route into its result Data as top-level Route,
/// which the chat navigates to after the turn. ShiftListViewRoutesUiMirrorGuardTests (Klacks.UnitTest) keeps both sides equal.
/// </summary>
public static class ShiftListViewRoutes
{
    public const string ShiftListRoute = "/workplace/shift";
    public const string ViewQueryParam = "view";
    public const string FromQueryParam = "from";
    public const string UntilQueryParam = "until";
    public const string OrdersView = "orders";
    public const string PlannableView = "plannable";

    public const string OrdersViewRoute = ShiftListRoute + "?" + ViewQueryParam + "=" + OrdersView;
    public const string PlannableViewRoute = ShiftListRoute + "?" + ViewQueryParam + "=" + PlannableView;
}
