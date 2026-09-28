// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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