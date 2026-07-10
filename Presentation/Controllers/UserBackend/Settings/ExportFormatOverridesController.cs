// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin endpoints for export format overrides (support hotfixes): catalog with stored overrides,
/// upsert/delete per format key, and a preview that renders the deterministic sample dataset with a
/// patch applied so support can verify the file before enabling the fix.
/// </summary>
using Klacks.Api.Application.Commands.Exports;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Exports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

[Authorize(Roles = Roles.Admin)]
public class ExportFormatOverridesController : BaseController
{
    private readonly IMediator _mediator;

    public ExportFormatOverridesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<ExportFormatOverrideCatalog>> List()
    {
        return Ok(await _mediator.Send(new ListExportFormatOverridesQuery()));
    }

    [HttpPut("{formatKey}")]
    public async Task<ActionResult<ExportFormatOverrideResource>> Save(string formatKey, [FromBody] ExportFormatOverrideSaveRequest request)
    {
        return Ok(await _mediator.Send(new SaveExportFormatOverrideCommand(formatKey, request.PatchJson, request.IsEnabled, request.Note)));
    }

    [HttpDelete("{formatKey}")]
    public async Task<ActionResult> Delete(string formatKey)
    {
        var deleted = await _mediator.Send(new DeleteExportFormatOverrideCommand(formatKey));
        return deleted ? NoContent() : NotFound();
    }

    [HttpPost("{formatKey}/preview")]
    public async Task<IActionResult> Preview(string formatKey, [FromBody] ExportFormatOverridePreviewRequest request)
    {
        var result = await _mediator.Send(new PreviewExportFormatOverrideQuery(formatKey, request.PatchJson));
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
