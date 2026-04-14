// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin-only REST surface for the Klacksy training review tool.
/// Exposes endpoints for browsing navigation targets, updating synonyms, and reviewing feedback.
/// </summary>
namespace Klacks.Api.Presentation.Controllers.Assistant;

using Klacks.Api.Application.Handlers.Klacksy;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/admin/klacksy-training")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public sealed class KlacksyTrainingController : ControllerBase
{
    private readonly IMediator _mediator;

    public KlacksyTrainingController(IMediator mediator) => _mediator = mediator;

    [HttpGet("targets")]
    public Task<IReadOnlyList<NavigationTarget>> GetTargets([FromQuery] string? status, [FromQuery] string? locale, CancellationToken ct)
        => _mediator.Send(new GetNavigationTargetsQuery(status, locale), ct);

    [HttpPut("targets/{targetId}/synonyms")]
    public Task<bool> UpdateSynonyms(string targetId, [FromBody] UpdateSynonymsDto dto, CancellationToken ct)
        => _mediator.Send(new UpdateNavigationTargetSynonymsCommand(targetId, dto.Locale, dto.Synonyms, dto.Status), ct);

    [HttpGet("feedback")]
    public Task<IReadOnlyList<KlacksyNavigationFeedback>> GetFeedback([FromQuery] string locale, [FromQuery] int take = 50, CancellationToken ct = default)
        => _mediator.Send(new GetNavigationFeedbackQuery(locale, take), ct);
}

public sealed record UpdateSynonymsDto(string Locale, string[] Synonyms, string Status);
