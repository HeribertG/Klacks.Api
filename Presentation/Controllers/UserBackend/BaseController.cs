namespace Klacks.Api.Presentation.Controllers.UserBackend;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Basecontroller.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/backend/[controller]")]
public class BaseController : ControllerBase
{
}
