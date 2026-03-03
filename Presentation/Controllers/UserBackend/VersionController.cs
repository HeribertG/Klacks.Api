// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;
    [ApiController]
    [Route("api/[controller]")]
    public class VersionController : ControllerBase
    {
        [HttpGet]
        public ActionResult GetVersion()
        {
            var version = new MyVersion();
            return Ok(new
            {
                MyVersion.Variant,
                MyVersion.Major,
                MyVersion.Minor,
                MyVersion.Patch,
                MyVersion.BuildKey,
                MyVersion.BuildTimestamp,
                VersionString = version.Get(),
                VersionStringWithBuildInfo = version.Get(true),
            });
        }
    }
