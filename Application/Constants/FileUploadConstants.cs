// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validation constants for image file uploads (allowed content types, server-side file names, magic-byte
/// signatures, maximum size, error messages). The content-type whitelist mirrors the frontend accept
/// attribute of the profile picture and logo/icon upload inputs; the file names mirror what the frontend
/// sends (settings-general, profile-picture and the report designer).
/// </summary>

using System.Net.Mime;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Constants;

public static class FileUploadConstants
{
    public const int MaxImageUploadSizeMegabytes = 5;
    public const long MaxImageUploadSizeBytes = MaxImageUploadSizeMegabytes * 1024L * 1024L;

    public const string LogoFileName = "own-logo.png";
    public const string IconFileName = "own-icon.ico";
    public const string ProfileFileSuffix = "profile";
    public const string ReportImagePrefix = "report-img-";
    public const string ReportImageExtension = ".png";
    public const string GuidFormat = "D";

    public const string PngExtension = ".png";
    public const string JpgExtension = ".jpg";
    public const string JpegExtension = ".jpeg";
    public const string GifExtension = ".gif";

    public const int SignatureHeaderLength = 8;

    public static readonly string[] AllowedImageContentTypes =
    [
        MediaTypeNames.Image.Jpeg,
        MediaTypeNames.Image.Png,
        MediaTypeNames.Image.Gif,
        MediaTypeNames.Image.Icon,
    ];

    public static readonly IReadOnlyDictionary<string, ImageFileFormat> ContentTypeFormats =
        new Dictionary<string, ImageFileFormat>(StringComparer.OrdinalIgnoreCase)
        {
            [MediaTypeNames.Image.Jpeg] = ImageFileFormat.Jpeg,
            [MediaTypeNames.Image.Png] = ImageFileFormat.Png,
            [MediaTypeNames.Image.Gif] = ImageFileFormat.Gif,
            [MediaTypeNames.Image.Icon] = ImageFileFormat.Ico,
        };

    public static readonly IReadOnlyList<(string Extension, ImageFileFormat Format)> ProfileExtensions =
    [
        (PngExtension, ImageFileFormat.Png),
        (JpgExtension, ImageFileFormat.Jpeg),
        (JpegExtension, ImageFileFormat.Jpeg),
        (GifExtension, ImageFileFormat.Gif),
    ];

    public static readonly IReadOnlySet<ImageFileFormat> PictureFormats =
        new HashSet<ImageFileFormat> { ImageFileFormat.Png, ImageFileFormat.Jpeg, ImageFileFormat.Gif };

    public static readonly IReadOnlySet<ImageFileFormat> IconFormats =
        new HashSet<ImageFileFormat> { ImageFileFormat.Ico, ImageFileFormat.Png };

    public static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    public static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    public static readonly byte[] Gif87Signature = "GIF87a"u8.ToArray();
    public static readonly byte[] Gif89Signature = "GIF89a"u8.ToArray();
    public static readonly byte[] IcoSignature = [0x00, 0x00, 0x01, 0x00];

    public static readonly string InvalidContentTypeMessage =
        $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageContentTypes)}.";

    public static readonly string FileTooLargeMessage =
        $"File size exceeds the maximum allowed size of {MaxImageUploadSizeMegabytes} MB.";

    public const string InvalidFileNameMessage = "The file name is not allowed.";

    public const string ContentMismatchMessage = "The file content does not match an allowed image format.";

    public const string FileNotFoundMessage = "File not found";

    public const string NoFileMessage = "No File";
}
