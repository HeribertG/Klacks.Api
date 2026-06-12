// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Serves the RFC 8414 authorization server metadata document under the well-known path
/// used by MCP clients (including claude.ai custom connectors) for authorization server discovery.
/// </summary>

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Queries.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.OAuth;

[ApiController]
[AllowAnonymous]
[Route(OAuthConstants.WellKnownSegment)]
public class OAuthAuthorizationServerMetadataController : ControllerBase
{
    private readonly IMediator _mediator;

    public OAuthAuthorizationServerMetadataController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet(OAuthConstants.AuthorizationServerMetadataEndpointName)]
    public async Task<ActionResult<AuthorizationServerMetadataResource>> GetAuthorizationServerMetadata()
    {
        var metadata = await _mediator.Send(new GetAuthorizationServerMetadataQuery());

        return Ok(metadata);
    }
}
