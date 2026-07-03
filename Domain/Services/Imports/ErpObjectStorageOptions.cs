// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Storage settings for ERP order import drop points. Provider selects the backing store:
/// FileSystem (default) stores files below RootPath on the server, S3 targets the single,
/// instance-wide S3-compatible bucket (one bucket, one prefix per customer/source system).
/// </summary>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Imports;

public class ErpObjectStorageOptions
{
    public const string SectionName = "ErpObjectStorage";

    public ErpObjectStorageProvider Provider { get; set; } = ErpObjectStorageProvider.FileSystem;

    public string RootPath { get; set; } = string.Empty;

    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = "eu-central-1";
}
