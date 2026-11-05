// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Machine-readable names of the three ways an ERP system hands order XML files to Klacks: copying them into the
/// drop point folder, an administrator uploading them on the Import settings page, and an HTTPS push with an ERP
/// import token.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpHandoverChannels
{
    public const string FolderDrop = "folder_drop";
    public const string AdminUpload = "admin_upload";
    public const string HttpsPush = "https_push";
}
