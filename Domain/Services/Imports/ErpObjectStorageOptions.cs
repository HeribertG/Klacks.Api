// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Connection settings for the single, instance-wide S3-compatible object storage bucket used
/// for ERP order import drop points (one bucket, one prefix per customer/source system).
/// </summary>
namespace Klacks.Api.Domain.Services.Imports;

public class ErpObjectStorageOptions
{
    public const string SectionName = "ErpObjectStorage";

    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = "eu-central-1";
}
