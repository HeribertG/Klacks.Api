// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin-only review surface for what Klacksy learned: the phrasings it picked up (plus the sharpened
/// descriptions the optimizer proposed), the capabilities it composed, and the wishes it still cannot
/// serve. Deliberately has no chat skill of its own - a skill that edits or deletes Klacksy's own learning
/// artefacts would let the assistant reinforce itself.
/// The JWT scheme is pinned explicitly on the class: AddIdentity overrides the runtime default to cookie
/// authentication, so a bare role gate would answer 401 to every JWT caller.
/// </summary>
/// <param name="mediator">Dispatches the learning queries and commands</param>

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Application.Queries.Assistant.Learning;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/learning")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class KlacksyLearningController : ControllerBase
{
    private const int DefaultLimit = 50;
    private const int MaxLimit = 200;

    private readonly IMediator _mediator;

    public KlacksyLearningController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("phrases")]
    public async Task<ActionResult<IReadOnlyList<LearnedPhraseDto>>> GetPhrases(
        [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var phrases = await _mediator.Send(new GetLearnedPhrasesQuery(Clamp(limit)), cancellationToken);
        return Ok(phrases);
    }

    [HttpPut("phrases/{id:guid}")]
    public async Task<IActionResult> UpdatePhrase(
        [FromRoute] Guid id,
        [FromBody] UpdateLearnedPhraseRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateLearnedPhraseCommand(id, request.Phrase, request.Description), cancellationToken);

        return Respond(result);
    }

    [HttpDelete("phrases/{id:guid}")]
    public async Task<IActionResult> DeletePhrase([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteLearnedPhraseCommand(id), cancellationToken);
        return Respond(result);
    }

    [HttpGet("capabilities")]
    public async Task<ActionResult<IReadOnlyList<LearnedCapabilityDto>>> GetCapabilities(
        [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var capabilities = await _mediator.Send(new GetLearnedCapabilitiesQuery(Clamp(limit)), cancellationToken);
        return Ok(capabilities);
    }

    [HttpPut("capabilities/{id:guid}")]
    public async Task<IActionResult> UpdateCapability(
        [FromRoute] Guid id,
        [FromBody] UpdateLearnedCapabilityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new UpdateLearnedCapabilityCommand(id, request.Goal, request.Synonyms), cancellationToken);

        return Respond(result);
    }

    [HttpDelete("capabilities/{id:guid}")]
    public async Task<IActionResult> DeleteCapability([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteLearnedCapabilityCommand(id), cancellationToken);
        return Respond(result);
    }

    [HttpGet("unfulfillable")]
    public async Task<ActionResult<IReadOnlyList<UnfulfillableWishDto>>> GetUnfulfillable(
        [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var wishes = await _mediator.Send(new GetUnfulfillableWishesQuery(Clamp(limit)), cancellationToken);
        return Ok(wishes);
    }

    [HttpDelete("unfulfillable/{id:guid}")]
    public async Task<IActionResult> DismissUnfulfillable([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DismissUnfulfillableWishCommand(id), cancellationToken);
        return Respond(result);
    }

    private static int Clamp(int? limit) => Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

    private IActionResult Respond(LearningMutationResult result)
    {
        if (!result.Found)
        {
            return NotFound();
        }

        if (result.Conflict)
        {
            return Conflict(new { error = result.Error });
        }

        if (result.Error != null)
        {
            return BadRequest(new { error = result.Error });
        }

        return NoContent();
    }
}
