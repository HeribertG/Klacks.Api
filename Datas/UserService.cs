using Klacks.Api.Constants;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Klacks.Api.Datas;

public class UserService : IUserService
{
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<AppUser> userManager;

    public UserService(IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.userManager = userManager;
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
            Console.WriteLine(ex.Message);
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
            Console.WriteLine(ex.Message);
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
