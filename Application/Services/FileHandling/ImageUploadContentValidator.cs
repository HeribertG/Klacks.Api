// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks an uploaded image by its leading bytes: the magic-byte format must be one the target slot accepts
/// (logo, profile and report images: PNG/JPEG/GIF; icon: ICO/PNG), must match the declared content type,
/// and for a profile picture must also match the extension the client chose. The fixed logo/icon/report names
/// carry a frontend-forced extension, so their extension is not compared with the content.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.FileHandling;

public static class ImageUploadContentValidator
{
    /// <summary>
    /// Checks whether an uploaded image matches its target, its declared content type and its file signature.
    /// </summary>
    /// <param name="target">The validated server-side target of the upload</param>
    /// <param name="contentType">The content type the client declared</param>
    /// <param name="header">The first bytes of the uploaded file (at least SignatureHeaderLength when available)</param>
    public static bool IsValid(ImageFileTarget target, string? contentType, ReadOnlySpan<byte> header)
    {
        var detected = DetectFormat(header);
        if (detected == null)
        {
            return false;
        }

        if (contentType == null
            || !FileUploadConstants.ContentTypeFormats.TryGetValue(contentType, out var declaredByContentType)
            || declaredByContentType != detected)
        {
            return false;
        }

        if (target.DeclaredFormat != null && target.DeclaredFormat != detected)
        {
            return false;
        }

        var allowedFormats = target.Slot == ImageFileSlot.Icon
            ? FileUploadConstants.IconFormats
            : FileUploadConstants.PictureFormats;

        return allowedFormats.Contains(detected.Value);
    }

    /// <summary>
    /// Detects the image format from the leading bytes of a file; null when no known signature matches.
    /// </summary>
    /// <param name="header">The first bytes of a file</param>
    public static ImageFileFormat? DetectFormat(ReadOnlySpan<byte> header)
    {
        if (header.StartsWith(FileUploadConstants.PngSignature))
        {
            return ImageFileFormat.Png;
        }

        if (header.StartsWith(FileUploadConstants.JpegSignature))
        {
            return ImageFileFormat.Jpeg;
        }

        if (header.StartsWith(FileUploadConstants.Gif87Signature) || header.StartsWith(FileUploadConstants.Gif89Signature))
        {
            return ImageFileFormat.Gif;
        }

        if (header.StartsWith(FileUploadConstants.IcoSignature))
        {
            return ImageFileFormat.Ico;
        }

        return null;
    }
}
