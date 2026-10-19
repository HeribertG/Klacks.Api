// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Security.Claims;
using Klacks.Api.Application.Commands.ErpDropPoints;
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Validation.Imports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Imports;

[Authorize(Roles = Roles.Admin)]
public class ErpDropPointsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<ErpDropPointsController> _logger;

    public ErpDropPointsController(IMediator mediator, ILogger<ErpDropPointsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IEnumerable<ErpDropPointListResource>> GetErpDropPoints()
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] GET ErpDropPoints");
        return await _mediator.Send(new ListQuery());
    }

    [HttpGet("default")]
    public async Task<ActionResult<ErpDropPointResource>> GetDefaultErpDropPoint()
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] GET ErpDropPoints/default");
        return await _mediator.Send(new GetDefaultQuery());
    }

    [HttpGet("files")]
    public async Task<ActionResult<ErpDropPointFilesResource>> GetDefaultErpDropPointFiles(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] GET ErpDropPoints/files");
        return await _mediator.Send(new GetDefaultFilesQuery(), cancellationToken);
    }

    [HttpPost("files")]
    [RequestSizeLimit(ErpOrderUploadConstants.MaxFileSizeBytes)]
    public async Task<IActionResult> UploadErpOrderFileToDefault(IFormFile? file, CancellationToken cancellationToken)
    {
        var validationError = ErpOrderFileUploadValidator.Validate(file?.FileName, file?.Length ?? 0);
        if (file == null || validationError != null)
        {
            return BadRequest(validationError ?? ErpOrderFileUploadValidator.EmptyFileError);
        }

        _logger.LogInformation("[ERP-DROP-POINT-API] POST ErpDropPoints/files - File: {FileName}", file.FileName.ForLog());

        await using var stream = file.OpenReadStream();
        var key = await _mediator.Send(new UploadErpOrderFileToDefaultCommand(file.FileName, stream, CurrentUserName()), cancellationToken);

        return Ok(new { key });
    }

    [HttpPost("files/{fileId:guid}/retry")]
    public async Task<ActionResult<ErpOrderFileRetryResultResource>> RetryErpOrderFile(Guid fileId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] POST ErpDropPoints/files/{FileId}/retry", fileId);
        return await _mediator.Send(new RetryErpOrderFileCommand(fileId), cancellationToken);
    }

    [HttpDelete("files/{fileId:guid}")]
    public async Task<IActionResult> DeleteErpOrderFile(Guid fileId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] DELETE ErpDropPoints/files/{FileId}", fileId);
        await _mediator.Send(new DeleteErpOrderFileCommand(fileId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ErpDropPointResource>> GetErpDropPoint(Guid id)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] GET ErpDropPoints/{Id}", id);
        var result = await _mediator.Send(new GetQuery(id));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpPost]
    public async Task<ActionResult<ErpDropPointResource>> PostErpDropPoint([FromBody] ErpDropPointResource resource)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] POST ErpDropPoints - Name: {Name}", resource.Name.ForLog());
        var result = await _mediator.Send(new PostCommand(resource));
        return result!;
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ErpDropPointResource>> PutErpDropPoint(Guid id, [FromBody] ErpDropPointResource resource)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] PUT ErpDropPoints - Id: {Id}, Name: {Name}", id, resource.Name.ForLog());
        resource.Id = id;
        var result = await _mediator.Send(new PutCommand(resource));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpPost("{id}/files")]
    [RequestSizeLimit(ErpOrderUploadConstants.MaxFileSizeBytes)]
    public async Task<IActionResult> UploadErpOrderFile(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        var validationError = ErpOrderFileUploadValidator.Validate(file?.FileName, file?.Length ?? 0);
        if (file == null || validationError != null)
        {
            return BadRequest(validationError ?? ErpOrderFileUploadValidator.EmptyFileError);
        }

        _logger.LogInformation("[ERP-DROP-POINT-API] POST ErpDropPoints/{Id}/files - File: {FileName}", id, file.FileName.ForLog());

        await using var stream = file.OpenReadStream();
        var key = await _mediator.Send(
            new UploadErpOrderFileCommand(id, file.FileName, stream, ErpImportChannel.UiUpload, CurrentUserName()),
            cancellationToken);

        return Ok(new { key });
    }

    [HttpPost("run")]
    public async Task<IActionResult> TriggerImportRun(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] POST ErpDropPoints/run");
        await _mediator.Send(new TriggerErpImportRunCommand(), cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ErpDropPointResource>> DeleteErpDropPoint(Guid id)
    {
        _logger.LogInformation("[ERP-DROP-POINT-API] DELETE ErpDropPoints/{Id}", id);
        var result = await _mediator.Send(new DeleteCommand(id));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    private string? CurrentUserName()
    {
        var name = User.FindFirst(ClaimTypes.Name)?.Value;
        return string.IsNullOrWhiteSpace(name) ? User.FindFirst(ClaimTypes.Email)?.Value : name;
    }
}
