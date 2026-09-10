// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface INavigationEntityRouteGuard
{
    bool RequiresEntity(string? targetId, string? route);
}
