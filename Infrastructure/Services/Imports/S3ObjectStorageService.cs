// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// S3-compatible object storage access for ERP order import drop points. Configured against a
/// single instance-wide bucket via ErpObjectStorageOptions (ServiceUrl set for non-AWS providers
/// such as Hetzner Object Storage; ForcePathStyle is required for most S3-compatible endpoints).
/// </summary>
using Amazon.S3;
using Amazon.S3.Model;
using Klacks.Api.Domain.Interfaces.Imports;
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
        var request = new ListObjectsV2Request
        {
            BucketName = _options.BucketName,
            Prefix = prefix
        };

        var keys = new List<string>();
        ListObjectsV2Response response;
        do
        {
            response = await _client.ListObjectsV2Async(request, cancellationToken);
            keys.AddRange(response.S3Objects.Select(o => o.Key));
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (response.IsTruncated == true);

        return keys;
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
}
