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
