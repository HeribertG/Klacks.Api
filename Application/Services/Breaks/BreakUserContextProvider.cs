// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Claims;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Breaks;

/// <summary>
/// Resolves user identity (admin flag, authorisation claim, user name) from the current HTTP context.
/// </summary>
public class BreakUserContextProvider : IBreakUserContextProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BreakUserContextProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public BreakUserContext GetUserContext()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return new BreakUserContext(
            IsAdmin: user?.IsInRole(Roles.Admin) == true,
            IsAuthorised: user?.HasClaim(ClaimNames.IsAuthorised, "true") == true,
            UserName: user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown");
    }
}
