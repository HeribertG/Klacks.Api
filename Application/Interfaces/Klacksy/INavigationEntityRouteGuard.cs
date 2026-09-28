// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Klacksy;

public interface INavigationEntityRouteGuard
{
    bool RequiresEntity(string? targetId, string? route);
}
