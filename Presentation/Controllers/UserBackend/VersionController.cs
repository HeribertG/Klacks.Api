// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
[Route("api/[controller]")]
public class VersionController : ControllerBase
{
    [HttpGet]
    public ActionResult GetVersion()
    {
        return Ok(new
        {
            MyVersion.Variant,
            MyVersion.Major,
            MyVersion.Minor,
            MyVersion.Patch,
            MyVersion.BuildKey,
            MyVersion.BuildTimestamp,
            VersionString = MyVersion.Get(),
            VersionStringWithBuildInfo = MyVersion.Get(true),
        });
    }
}
