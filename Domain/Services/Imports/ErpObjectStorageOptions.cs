// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Storage settings for ERP order import drop points. Files are stored below RootPath
/// on the server via the FileSystem object storage backend.
/// </summary>
/// <param name="RootPath">Storage root as the Klacks process sees it (relative paths resolve against the app directory)</param>
/// <param name="HostPathHint">Docker only: absolute host folder that the compose file binds onto RootPath, shown to
/// administrators instead of the container path; empty when the compose file does not report it</param>
namespace Klacks.Api.Domain.Services.Imports;

public class ErpObjectStorageOptions
{
    public const string SectionName = "ErpObjectStorage";

    public const string DefaultRootPath = "ErpImport";

    public string RootPath { get; set; } = string.Empty;

    public string HostPathHint { get; set; } = string.Empty;
}
