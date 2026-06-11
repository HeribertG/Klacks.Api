// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validation constants for image file uploads (allowed content types, maximum size, error messages).
/// The content-type whitelist mirrors the frontend accept attribute of the profile picture and logo/icon upload inputs.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class FileUploadConstants
{
    public const int MaxImageUploadSizeMegabytes = 5;
    public const long MaxImageUploadSizeBytes = MaxImageUploadSizeMegabytes * 1024L * 1024L;

    public static readonly string[] AllowedImageContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/gif",
        "image/x-icon",
    ];

    public static readonly string InvalidContentTypeMessage =
        $"Invalid file type. Allowed types: {string.Join(", ", AllowedImageContentTypes)}.";

    public static readonly string FileTooLargeMessage =
        $"File size exceeds the maximum allowed size of {MaxImageUploadSizeMegabytes} MB.";
}
