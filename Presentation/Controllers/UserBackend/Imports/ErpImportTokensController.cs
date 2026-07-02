// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Manages ERP import tokens (upload credentials for a single drop point). Admin-only: minting
/// or revoking one of these tokens is a security-sensitive action, same as personal access tokens.
/// </summary>
using Klacks.Api.Application.Commands.ErpImportTokens;
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Queries.ErpImportTokens;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Imports;

[ApiController]
[Route("api/backend/erp-import-tokens")]
[Authorize(Roles = Roles.Admin)]
public class ErpImportTokensController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<ErpImportTokensController> _logger;

    public ErpImportTokensController(IMediator mediator, ILogger<ErpImportTokensController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<ErpImportTokenListItemDto>>> GetForDropPoint([FromQuery] Guid dropPointId)
    {
        var tokens = await _mediator.Send(new GetErpImportTokensQuery(dropPointId));
        return Ok(tokens);
    }

    [HttpPost]
    public async Task<ActionResult<ErpImportTokenCreatedDto>> Create([FromBody] CreateErpImportTokenRequest request)
    {
        var created = await _mediator.Send(new CreateErpImportTokenCommand(request.DropPointId, request.Name, request.ExpiresInDays));

        _logger.LogInformation("ERP import token {TokenId} created for drop point {DropPointId}", created.Id, created.DropPointId);

        return Ok(created);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(Guid id, [FromQuery] Guid dropPointId)
    {
        var revoked = await _mediator.Send(new RevokeErpImportTokenCommand(id, dropPointId));
        if (!revoked)
        {
            return NotFound();
        }

        _logger.LogInformation("ERP import token {TokenId} revoked", id);

        return NoContent();
    }
}

public record CreateErpImportTokenRequest(Guid DropPointId, string Name, int? ExpiresInDays);
