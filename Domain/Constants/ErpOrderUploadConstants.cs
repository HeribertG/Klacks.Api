// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared constants for ERP order file uploads: the request size limit applied to both the
/// token-authenticated ERP upload endpoint and the admin drop-point upload endpoint, and the
/// only accepted file extension for admin uploads.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpOrderUploadConstants
{
    public const long MaxFileSizeBytes = 50_000_000;

    public const string XmlFileExtension = ".xml";
}
