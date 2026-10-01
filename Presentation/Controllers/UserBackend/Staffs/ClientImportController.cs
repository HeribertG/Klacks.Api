// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Employee import from an xlsx/csv list: Parse reads the upload into a grid with a proposed mapping,
/// Preview shows every row as it would be written, Commit writes all ready rows in one transaction and
/// Template downloads an empty list with headers in the caller's language. Admin only (owner decision
/// 2026-10-01): a bulk import sees every existing employee for its duplicate check and every group, which
/// only an unrestricted caller may. The upload is buffered in memory and never stored; Preview and Commit
/// are rate limited per user because each call re-evaluates the whole grid. Parse reads the form itself
/// and takes no parameters at all (any parameter, even a CancellationToken, makes MVC read the form in its
/// value providers before the action runs), so an upload above the size limits is answered with the file-too-large code and
/// not with the framework's code-less 400; an unreadable Preview/Commit body is answered with
/// invalid-request (ClientImportInvalidModelStateFilterAttribute). Template falls back to the request's
/// Accept-Language when no language is given.
/// </summary>
/// <param name="mediator">Dispatches the import commands and queries</param>

using Klacks.Api.Application.Commands.ClientImport;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Queries.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

[ApiController]
[ClientImportInvalidModelStateFilter]
public class ClientImportController : BaseController
{
    private const string AllowedRoles = Roles.Admin;
    private const string FileFormField = "file";
    private const string SheetNameFormField = "sheetName";

    private readonly IMediator _mediator;

    public ClientImportController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Parse")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AllowedRoles)]
    [EnableRateLimiting(RateLimitingPolicies.Upload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ClientImportLimits.MaxUploadRequestBytes)]
    [RequestFormLimits(
        MultipartBodyLengthLimit = ClientImportLimits.MaxUploadRequestBytes,
        MemoryBufferThreshold = ClientImportLimits.UploadMemoryBufferBytes)]
    public async Task<ActionResult<ClientImportParseResult>> Parse()
    {
        var cancellationToken = HttpContext.RequestAborted;
        var form = await ReadUploadFormAsync(cancellationToken);
        var file = form.Files.GetFile(FileFormField);
        var sheetName = form.TryGetValue(SheetNameFormField, out var sheetValue) && !string.IsNullOrEmpty(sheetValue.ToString())
            ? sheetValue.ToString()
            : null;

        if (file == null || file.Length == 0)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileEmpty, "No file was uploaded.");
        }

        using var buffer = new MemoryStream();
        await using (var stream = file.OpenReadStream())
        {
            await stream.CopyToAsync(buffer, cancellationToken);
        }

        var result = await _mediator.Send(new ParseClientImportCommand(buffer.ToArray(), file.FileName, sheetName), cancellationToken);
        return Ok(result);
    }

    [HttpPost("Preview")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AllowedRoles)]
    [EnableRateLimiting(RateLimitingPolicies.ClientImport)]
    [RequestSizeLimit(ClientImportLimits.MaxRequestBytes)]
    public async Task<ActionResult<ClientImportPreviewResult>> Preview([FromBody] ClientImportRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new PreviewClientImportQuery(request), cancellationToken);
        return Ok(result);
    }

    [HttpPost("Commit")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AllowedRoles)]
    [EnableRateLimiting(RateLimitingPolicies.ClientImport)]
    [RequestSizeLimit(ClientImportLimits.MaxRequestBytes)]
    public async Task<ActionResult<ClientImportCommitResult>> Commit([FromBody] ClientImportRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CommitClientImportCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("Template")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = AllowedRoles)]
    public async Task<IActionResult> Template([FromQuery] string? language, CancellationToken cancellationToken)
    {
        var preferredLanguages = Request.GetTypedHeaders().AcceptLanguage
            .Where(value => value.Value.HasValue)
            .OrderByDescending(value => value.Quality ?? 1)
            .Select(value => value.Value.Value!)
            .ToList();

        var template = await _mediator.Send(new GetClientImportTemplateQuery(language, preferredLanguages), cancellationToken);
        return File(template.Content, template.ContentType, template.FileName);
    }

    private async Task<IFormCollection> ReadUploadFormAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await Request.ReadFormAsync(cancellationToken);
        }
        catch (BadHttpRequestException ex) when (ex.StatusCode == StatusCodes.Status413PayloadTooLarge)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileTooLarge, $"The upload exceeds {ClientImportLimits.MaxUploadRequestBytes} bytes.", ex);
        }
        catch (BadHttpRequestException ex)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.InvalidRequest, "The upload could not be read.", ex);
        }
        catch (InvalidDataException ex) when (Request.ContentLength is null or > ClientImportLimits.MaxUploadRequestBytes)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileTooLarge, $"The upload exceeds {ClientImportLimits.MaxUploadRequestBytes} bytes.", ex);
        }
        catch (InvalidDataException ex)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.InvalidRequest, "The upload is not a valid multipart form.", ex);
        }
    }
}
