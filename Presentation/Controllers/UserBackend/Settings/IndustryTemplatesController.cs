// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only preview of an industry profile's scheduling rules and qualifications, independent of
/// which industry is currently active via ACTIVE_INDUSTRIES - lets an admin see what a profile
/// would bring before selecting it. Also exposes a custom-rules summary used to warn before
/// switching ACTIVE_INDUSTRIES away from "custom" when custom scheduling rules already exist.
/// </summary>

using Klacks.Api.Application.DTOs.IndustryTemplates;
using Klacks.Api.Application.Queries.IndustryTemplates;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

[Authorize(Roles = Roles.Admin)]
public class IndustryTemplatesController : BaseController
{
    private readonly IMediator _mediator;

    public IndustryTemplatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("GetPreview/{slug}")]
    public async Task<IndustryTemplatePreviewResource> GetPreview(string slug)
    {
        return await _mediator.Send(new PreviewQuery(slug));
    }

    [HttpGet("GetCustomRulesSummary")]
    public async Task<CustomRulesSummaryResource> GetCustomRulesSummary()
    {
        return await _mediator.Send(new CustomRulesSummaryQuery());
    }
}
