// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Upload, download and delete of the company logo/icon, report designer images and the caller's own profile
/// picture. File names are resolved against a server-side whitelist (ImageFileNamePolicy); logo, icon and
/// report images require CanEditSettings, a profile picture may only be changed by its owner. Uploads are
/// checked by content type, size and magic bytes before they are stored under the validated name.
/// </summary>
/// <param name="fileUploadService">Stores, reads and deletes the images on disk</param>
/// <param name="logger">Logs download failures without returning their details to the client</param>

using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.FileHandling;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Presentation.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[EnableRateLimiting(RateLimitingPolicies.Upload)]
public class LoadFileController : BaseController
{
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<LoadFileController> _logger;

    public LoadFileController(IFileUploadService fileUploadService, ILogger<LoadFileController> logger)
    {
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    [HttpDelete("{type}")]
    public ActionResult DeleteFile(string type)
    {
        var target = ImageFileNamePolicy.ResolveStored(type);
        if (target == null)
        {
            return BadRequest(FileUploadConstants.InvalidFileNameMessage);
        }

        if (!MayChange(target))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        _fileUploadService.DeleteImage(target);
        return Ok();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("Upload")]
    [Consumes(MediaTypeNames.Multipart.FormData)]
    public async Task<ActionResult> SingleFile([FromForm] IFormFile file)
    {
        if (file == null)
        {
            return Ok(FileUploadConstants.NoFileMessage);
        }

        var target = ImageFileNamePolicy.ResolveUpload(file.FileName);
        if (target == null)
        {
            return BadRequest(FileUploadConstants.InvalidFileNameMessage);
        }

        if (!MayChange(target))
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        if (!FileUploadConstants.AllowedImageContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(FileUploadConstants.InvalidContentTypeMessage);
        }

        if (file.Length > FileUploadConstants.MaxImageUploadSizeBytes)
        {
            return BadRequest(FileUploadConstants.FileTooLargeMessage);
        }

        var header = await ReadHeaderAsync(file, HttpContext.RequestAborted);
        if (!ImageUploadContentValidator.IsValid(target, file.ContentType, header))
        {
            return BadRequest(FileUploadConstants.ContentMismatchMessage);
        }

        await using var content = file.OpenReadStream();
        await _fileUploadService.StoreImageAsync(target, content, HttpContext.RequestAborted);
        return Ok();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("DownLoad")]
    public async Task<FileContentResult> SingleFile(string type)
    {
        var target = ImageFileNamePolicy.ResolveStored(type);
        if (target == null)
        {
            return NotFoundFile();
        }

        try
        {
            var content = await _fileUploadService.ReadImageAsync(target, HttpContext.RequestAborted);
            return content == null ? NotFoundFile() : File(content, MediaTypeNames.Image.Png);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Reading image {Slot} failed", target.Slot);
            return NotFoundFile();
        }
    }

    private FileContentResult NotFoundFile()
    {
        return File(Encoding.UTF8.GetBytes(FileUploadConstants.FileNotFoundMessage), MediaTypeNames.Text.Plain);
    }

    private bool MayChange(ImageFileTarget target)
    {
        if (target.Slot != ImageFileSlot.Profile)
        {
            return Permissions.HasPermission(User.GetUserRights(), Permissions.CanEditSettings);
        }

        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(callerId, out var callerGuid) && callerGuid == target.OwnerUserId;
    }

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var buffer = new byte[FileUploadConstants.SignatureHeaderLength];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        return buffer[..read];
    }
}
