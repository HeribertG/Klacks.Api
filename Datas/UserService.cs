
using System.Security.Claims;
using Klacks_api.Interfaces;

namespace Klacks_api.Datas;

public class UserService : IUserService
{
  private readonly IHttpContextAccessor httpContextAccessor;

  public UserService(IHttpContextAccessor httpContextAccessor)
  {
    this.httpContextAccessor = httpContextAccessor;
  }

  public Guid? GetId()
  {
    Guid? currentUserId = null;
    try
    {
      if (httpContextAccessor.HttpContext != null)
      {
        if (httpContextAccessor.HttpContext.User?.FindFirst(x => x.Type == "Id") != null)
        {
#pragma warning disable CS8602 // Dereferenzierung eines m�glichen Nullverweises.
          var id = !string.IsNullOrEmpty(httpContextAccessor.HttpContext.User?.FindFirst(x => x.Type == "Id").Value) ? httpContextAccessor.HttpContext.User?.FindFirst(x => x.Type == "Id").Value : null;
#pragma warning restore CS8602 // Dereferenzierung eines m�glichen Nullverweises.

          if (id != null) { currentUserId = Guid.Parse(id); }

        }
      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.Message);
    }

    return currentUserId;
  }

  public string GetUserName()
  {
    var currentUserName = "Anonymous";
    try
    {
      if (httpContextAccessor.HttpContext != null)
      {
        if (httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier) != null)
        {
#pragma warning disable CS8602 // Dereferenzierung eines m�glichen Nullverweises.
          currentUserName = !string.IsNullOrEmpty(httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier).Value) ? httpContextAccessor.HttpContext.User?.FindFirst(ClaimTypes.NameIdentifier).Value : "Anonymous";
#pragma warning restore CS8602 // Dereferenzierung eines m�glichen Nullverweises.

        }

      }
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.Message);
    }

    return currentUserName!;
  }
}
