// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Server-side whitelist of the image file names the LoadFile endpoints accept: the company logo, the
/// company icon, a report designer image ("report-img-{guid}.png") and a user's profile picture
/// ("{userId}profile" plus an allowed extension on upload, without extension on download/delete).
/// Every other name resolves to null.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.FileHandling;

public static class ImageFileNamePolicy
{
    /// <param name="fileName">The file name the client sent with the multipart upload</param>
    public static ImageFileTarget? ResolveUpload(string? fileName)
    {
        var fixedTarget = ResolveFixedName(fileName);
        if (fixedTarget != null)
        {
            return fixedTarget;
        }

        var extension = Path.GetExtension(fileName);
        var profileExtension = FileUploadConstants.ProfileExtensions
            .FirstOrDefault(entry => string.Equals(entry.Extension, extension, StringComparison.OrdinalIgnoreCase));
        if (profileExtension.Extension == null)
        {
            return null;
        }

        var ownerUserId = ParseProfileOwner(Path.GetFileNameWithoutExtension(fileName));
        if (ownerUserId == null)
        {
            return null;
        }

        var canonicalName = ProfileBaseName(ownerUserId.Value) + profileExtension.Extension;
        if (!string.Equals(fileName, canonicalName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return BuildProfileTarget(ownerUserId.Value, canonicalName, profileExtension.Format);
    }

    /// <param name="name">The name the client sent to the download or delete endpoint</param>
    public static ImageFileTarget? ResolveStored(string? name)
    {
        var fixedTarget = ResolveFixedName(name);
        if (fixedTarget != null)
        {
            return fixedTarget;
        }

        var ownerUserId = ParseProfileOwner(name);
        if (ownerUserId == null || !string.Equals(name, ProfileBaseName(ownerUserId.Value), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var storedNames = ProfileStoredNames(ownerUserId.Value);
        return BuildProfileTarget(ownerUserId.Value, storedNames[0], null);
    }

    private static ImageFileTarget? ResolveFixedName(string? name)
    {
        if (string.Equals(name, FileUploadConstants.LogoFileName, StringComparison.Ordinal))
        {
            return FixedTarget(ImageFileSlot.Logo, FileUploadConstants.LogoFileName);
        }

        if (string.Equals(name, FileUploadConstants.IconFileName, StringComparison.Ordinal))
        {
            return FixedTarget(ImageFileSlot.Icon, FileUploadConstants.IconFileName);
        }

        var reportImageId = ParseReportImageId(name);
        if (reportImageId != null)
        {
            var reportImageName = FileUploadConstants.ReportImagePrefix
                + reportImageId.Value.ToString(FileUploadConstants.GuidFormat)
                + FileUploadConstants.ReportImageExtension;
            return string.Equals(name, reportImageName, StringComparison.OrdinalIgnoreCase)
                ? FixedTarget(ImageFileSlot.ReportImage, reportImageName)
                : null;
        }

        return null;
    }

    private static Guid? ParseReportImageId(string? name)
    {
        if (name == null
            || !name.StartsWith(FileUploadConstants.ReportImagePrefix, StringComparison.Ordinal)
            || !name.EndsWith(FileUploadConstants.ReportImageExtension, StringComparison.Ordinal))
        {
            return null;
        }

        var idPart = name[FileUploadConstants.ReportImagePrefix.Length..^FileUploadConstants.ReportImageExtension.Length];
        return Guid.TryParseExact(idPart, FileUploadConstants.GuidFormat, out var id) ? id : null;
    }

    private static Guid? ParseProfileOwner(string? baseName)
    {
        if (baseName == null || !baseName.EndsWith(FileUploadConstants.ProfileFileSuffix, StringComparison.Ordinal))
        {
            return null;
        }

        var idPart = baseName[..^FileUploadConstants.ProfileFileSuffix.Length];
        return Guid.TryParseExact(idPart, FileUploadConstants.GuidFormat, out var id) ? id : null;
    }

    private static string ProfileBaseName(Guid ownerUserId)
    {
        return ownerUserId.ToString(FileUploadConstants.GuidFormat) + FileUploadConstants.ProfileFileSuffix;
    }

    private static List<string> ProfileStoredNames(Guid ownerUserId)
    {
        var baseName = ProfileBaseName(ownerUserId);
        return FileUploadConstants.ProfileExtensions.Select(entry => baseName + entry.Extension).ToList();
    }

    private static ImageFileTarget BuildProfileTarget(Guid ownerUserId, string fileName, ImageFileFormat? declaredFormat)
    {
        return new ImageFileTarget(ImageFileSlot.Profile, ownerUserId, fileName, ProfileStoredNames(ownerUserId), declaredFormat);
    }

    private static ImageFileTarget FixedTarget(ImageFileSlot slot, string fileName)
    {
        return new ImageFileTarget(slot, null, fileName, [fileName], null);
    }
}
