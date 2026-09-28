// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Admin-only review surface for what Klacksy learned: the phrasings it picked up (plus the sharpened
/// descriptions the optimizer proposed), the capabilities it composed, and the wishes it still cannot
/// serve. Deliberately has no chat skill of its own - a skill that edits or deletes Klacksy's own learning
/// artefacts would let the assistant reinforce itself.
/// The JWT scheme is pinned explicitly on the class: AddIdentity overrides the runtime default to cookie
/// authentication, so a bare role gate would answer 401 to every JWT caller.
/// The export surface (export-candidates, mark-exported, run-status) serves scripts/export-learned-descriptions.ps1
/// and scripts/weekly-learning-pipeline.ps1 on the dev machine.
/// </summary>
/// <param name="mediator">Dispatches the learning queries and commands</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
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

    private static readonly string ProposalIdsRangeError =
        $"proposalIds must name between 1 and {SkillLearningDefaults.MaxExportCandidates} proposals.";

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

    /// <summary>
    /// Overrides the routing regression gate on a blocked description proposal, shown on the card next to
    /// the learned phrases. The reviewer comes from the JWT: an approvable proposal changes a live skill
    /// description, so who did it must be recorded, not merely known to be an admin.
    /// </summary>
    [HttpPost("phrases/{id:guid}/approve")]
    public async Task<IActionResult> ApprovePhraseProposal([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new ApproveProposedSkillChangeCommand { ProposalId = id, ReviewedBy = reviewedBy }, cancellationToken);
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

    /// <summary>
    /// Hands a wish the loop gave up on back to the learning loop, with a fresh attempt budget. The way
    /// out of unfulfillable the state machine always described, now reachable.
    /// </summary>
    [HttpPost("unfulfillable/{id:guid}/retry")]
    public async Task<IActionResult> RetryUnfulfillable([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RetryUnfulfillableWishCommand(id), cancellationToken);
        return Respond(result);
    }

    /// <summary>
    /// Starts a learning run now instead of waiting for the scheduled one. Answers as soon as the run is
    /// under way, because a run rebuilds the knowledge index several times; a run that was already going
    /// is reported as not started rather than queued behind the first.
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<SkillLearningRunResponse>> Run(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RunSkillLearningCommand(), cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Whether a learning run is under way and how the latest one ended. POST run starts detached, so a script
    /// polls here to know that the run finished before it exports anything.
    /// </summary>
    [HttpGet("run-status")]
    public async Task<ActionResult<SkillLearningRunStatusResponse>> GetRunStatus(CancellationToken cancellationToken)
    {
        var status = await _mediator.Send(new GetSkillLearningRunStatusQuery(), cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// The gate-passed description proposals the export script writes into skill-seeds.json.
    /// </summary>
    [HttpGet("export-candidates")]
    public async Task<ActionResult<IReadOnlyList<LearnedDescriptionExportCandidateDto>>> GetExportCandidates(
        [FromQuery] int? limit, CancellationToken cancellationToken)
    {
        var effectiveLimit = Math.Clamp(limit ?? SkillLearningDefaults.MaxExportCandidates, 1, SkillLearningDefaults.MaxExportCandidates);
        var candidates = await _mediator.Send(
            new GetLearnedDescriptionExportCandidatesQuery(effectiveLimit), cancellationToken);
        return Ok(candidates);
    }

    /// <summary>
    /// Marks proposals as exported after the export script wrote them. The reviewer comes from the JWT.
    /// </summary>
    [HttpPost("mark-exported")]
    public async Task<ActionResult<MarkProposalsExportedResult>> MarkExported(
        [FromBody] MarkProposalsExportedRequest request, CancellationToken cancellationToken)
    {
        var reviewedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(reviewedBy))
        {
            return Unauthorized();
        }

        if (request.ProposalIds == null
            || request.ProposalIds.Count == 0
            || request.ProposalIds.Count > SkillLearningDefaults.MaxExportCandidates)
        {
            return BadRequest(new { error = ProposalIdsRangeError });
        }

        var result = await _mediator.Send(
            new MarkProposalsExportedCommand(request.ProposalIds, reviewedBy), cancellationToken);
        return Ok(result);
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
