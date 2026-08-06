// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISelfApiRouteResolver
{
    /// <summary>Route of the controller serving this resource, e.g. "api/backend/Expenses".</summary>
    /// <param name="resourceType">The resource DTO the controller is typed on</param>
    string Resolve(Type resourceType);

    bool TryResolve(Type resourceType, out string route);
}
