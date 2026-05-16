// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exposes the autonomy-roadmap S10 coverage metric (covered / partial / missing use cases
/// from docs/klacksy-usecases.md) so the Settings UI can render a live trend.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/coverage")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SkillCoverageController : ControllerBase
{
    private readonly ISkillCoverageService _coverageService;

    public SkillCoverageController(ISkillCoverageService coverageService)
    {
        _coverageService = coverageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCoverage(CancellationToken cancellationToken)
    {
        var report = await _coverageService.ComputeAsync(cancellationToken);
        return Ok(report);
    }
}
