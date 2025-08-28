using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Web
{
    /// <summary>
    /// Controller for password reset web pages
    /// </summary>
    public class PasswordResetController : BaseWebController
    {
        [HttpGet("reset-password")]
        public IActionResult ResetPassword(string token)
        {
            ViewData["Token"] = token;
            return View("ResetPassword");
        }
    }
}