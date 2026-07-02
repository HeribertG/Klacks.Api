// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Upload endpoint for ERP order files. Deliberately does NOT inherit BaseController (which
/// pins JWT-bearer auth) -- this endpoint is scheme-pinned to KlacksErpImport only, since the
/// caller is an external ERP integration holding a drop-point-scoped import token, never a
/// logged-in Klacks user.
/// </summary>
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Imports;

[ApiController]
[Route("api/erp-import")]
[Authorize(AuthenticationSchemes = ErpImportTokenConstants.SchemeName)]
public class ErpOrderUploadController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ErpOrderUploadController> _logger;

    public ErpOrderUploadController(IMediator mediator, ILogger<ErpOrderUploadController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(50_000_000)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        var dropPointIdClaim = User.FindFirst(ErpImportTokenConstants.DropPointIdClaimType)?.Value;
        if (!Guid.TryParse(dropPointIdClaim, out var dropPointId))
        {
            return Unauthorized();
        }

        if (file.Length == 0)
        {
            return BadRequest("File must not be empty.");
        }

        _logger.LogInformation("[ERP-IMPORT-UPLOAD] Upload for drop point {DropPointId}, file {FileName}", dropPointId, file.FileName.ForLog());

        await using var stream = file.OpenReadStream();
        var key = await _mediator.Send(new UploadErpOrderFileCommand(dropPointId, file.FileName, stream), cancellationToken);

        return Ok(new { key });
    }
}
