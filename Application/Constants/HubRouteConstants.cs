// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Path prefix under which every SignalR hub is mapped. Shared by the token selection of JwtBearer, the SignalR
/// query-string authentication and the native Ui hosting, which must not serve the Ui fallback for hub requests.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class HubRouteConstants
{
    public const string HubPathPrefix = "/hubs";
}
