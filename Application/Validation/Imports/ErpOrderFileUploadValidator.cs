// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates metadata of an admin-uploaded ERP order file before the upload command is sent.
/// Returns an English error message or null when the file is acceptable.
/// </summary>
/// <param name="fileName">The original file name whose extension must be .xml (case-insensitive)</param>
/// <param name="fileLength">The file size in bytes; must be greater than zero and within the shared upload limit</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Validation.Imports;

public static class ErpOrderFileUploadValidator
{
    public const string EmptyFileError = "File must not be empty.";
    public const string EmptyFileNameError = "File name must not be empty.";
    public const string InvalidExtensionError = "Only .xml files are allowed.";
    public const string FileTooLargeError = "File exceeds the maximum allowed size.";

    public static string? Validate(string? fileName, long fileLength)
    {
        if (fileLength <= 0)
        {
            return EmptyFileError;
        }

        var extension = Path.GetExtension(fileName);
        if (!string.Equals(extension, ErpOrderUploadConstants.XmlFileExtension, StringComparison.OrdinalIgnoreCase))
        {
            return InvalidExtensionError;
        }

        if (fileLength > ErpOrderUploadConstants.MaxFileSizeBytes)
        {
            return FileTooLargeError;
        }

        return null;
    }
}
