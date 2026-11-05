// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Machine-readable names of what has to be arranged before an ERP system can write into the drop point folder.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpFolderDropRequirements
{
    public const string GrantWriteAccessOnFolder = "grant_write_access_on_folder";
    public const string MountHostFolder = "mount_host_folder";
}
