// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Web
{
    /// <summary>
    /// Base controller for web pages (not API endpoints)
    /// </summary>
    [Route("")]
    public class BaseWebController : Controller
    {
    }
}