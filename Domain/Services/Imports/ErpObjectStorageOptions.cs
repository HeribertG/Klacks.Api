// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Storage settings for ERP order import drop points. Files are stored below RootPath
/// on the server via the FileSystem object storage backend.
/// </summary>
namespace Klacks.Api.Domain.Services.Imports;

public class ErpObjectStorageOptions
{
    public const string SectionName = "ErpObjectStorage";

    public string RootPath { get; set; } = string.Empty;
}
