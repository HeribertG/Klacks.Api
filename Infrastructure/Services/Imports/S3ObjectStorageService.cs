// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// S3-compatible object storage access for ERP order import drop points. Configured against a
/// single instance-wide bucket via ErpObjectStorageOptions (ServiceUrl set for non-AWS providers
/// such as Hetzner Object Storage; ForcePathStyle is required for most S3-compatible endpoints).
/// </summary>
using Amazon.S3;
using Amazon.S3.Model;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Imports;

public class S3ObjectStorageService : IObjectStorageService
{
    private readonly ErpObjectStorageOptions _options;
    private readonly IAmazonS3 _client;

    public S3ObjectStorageService(IAmazonS3 client, IOptions<ErpObjectStorageOptions> options)
    {
        _client = client;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var objects = await ListObjectsAsync(prefix, cancellationToken);
        return objects.Select(o => o.Key).ToList();
    }

    public async Task<IReadOnlyList<StorageObjectMetadata>> ListWithMetadataAsync(string prefix, CancellationToken cancellationToken = default)
    {
        var objects = await ListObjectsAsync(prefix, cancellationToken);
        return objects
            .Select(o => new StorageObjectMetadata(
                o.Key,
                o.Size ?? 0,
                (o.LastModified ?? DateTime.MinValue).ToUniversalTime()))
            .ToList();
    }

    private async Task<List<S3Object>> ListObjectsAsync(string prefix, CancellationToken cancellationToken)
    {
        var request = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = prefix
        };

        var objects = new List<S3Object>();
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request, cancellationToken);
            objects.AddRange(response.S3Objects);
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);

        return objects;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetObjectAsync(_options.BucketName, key, cancellationToken);
        return response.ResponseStream;
    }

    public async Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default)
    {
        await _client.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = _options.BucketName,
            SourceKey = sourceKey,
            DestinationBucket = _options.BucketName,
            DestinationKey = destinationKey
        }, cancellationToken);

        await _client.DeleteObjectAsync(_options.BucketName, sourceKey, cancellationToken);
    }

    public async Task UploadAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content
        }, cancellationToken);
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_options.BucketName, key, cancellationToken);
    }
}
