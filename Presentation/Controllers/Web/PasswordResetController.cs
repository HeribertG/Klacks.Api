// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Web
{
   
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