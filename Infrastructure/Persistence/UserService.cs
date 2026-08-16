// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Klacks.Api.Infrastructure.Persistence;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<AppUser> userManager;
    private readonly ILogger<UserService> _logger;

    public UserService(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager, ILogger<UserService> logger)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
        _logger = logger;
    }

    public Guid? GetId()
    {
        Guid? currentUserId = null;
        var id = GetIdString();
        if (id != null)
        {
            currentUserId = Guid.Parse(id);
        }
        
        return currentUserId;
    }

    public string? GetIdString()
    {
        try
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim?.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user ID from claims");
            return null;
        }
    }

    public string GetUserName()
    {
        try
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name);
            return claim?.Value ?? "Anonymous";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving username from claims");
            return "Anonymous";
        }
    }

    public string GetDisplayName()
    {
        try
        {
            var user = httpContextAccessor.HttpContext?.User;
            var givenName = user?.FindFirst(ClaimTypes.GivenName)?.Value ?? string.Empty;
            var surname = user?.FindFirst(ClaimTypes.Surname)?.Value ?? string.Empty;
            var displayName = $"{givenName} {surname}".Trim();

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                return displayName;
            }

            var name = user?.FindFirst(ClaimTypes.Name)?.Value;
            return string.IsNullOrWhiteSpace(name) ? AuditActorDefaults.UnknownActor : name;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving display name from claims");
            return AuditActorDefaults.UnknownActor;
        }
    }

    public string? GetInstanceId()
    {
        try
        {
            var header = httpContextAccessor.HttpContext?.Request?.Headers["X-Instance-Id"].ToString();
            return string.IsNullOrWhiteSpace(header) ? null : header;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving instance id from headers");
            return null;
        }
    }

    public async Task<bool> IsAdmin()
    {
        var userId = GetIdString();
        
        if (!string.IsNullOrEmpty(userId))
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var roles = await userManager.GetRolesAsync(user);
                return roles.Contains(Roles.Admin);
             }
        }

        return false;
    }
}
