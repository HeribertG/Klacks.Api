namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Basecontroller.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/v1/backend/[controller]")]
public class BaseController : ControllerBase
{
}
