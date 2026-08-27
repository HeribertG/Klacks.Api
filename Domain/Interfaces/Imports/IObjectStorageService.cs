// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IObjectStorageService
{
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageObjectMetadata>> ListWithMetadataAsync(string prefix, CancellationToken cancellationToken = default);

    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default);

    Task MoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an object only if the source still exists and the destination is still free, and reports
    /// whether this caller won. Implementations must make the check and the move a single atomic step so
    /// concurrent callers -- a second API instance, or an overlapping run -- cannot both claim one object.
    /// </summary>
    /// <param name="sourceKey">Key of the object to claim; a key another caller already claimed yields false</param>
    /// <param name="destinationKey">Key the object is moved to while it is owned by the winning caller</param>
    Task<bool> TryClaimAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);

    Task UploadAsync(string key, Stream content, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the storage root is reachable and writable via a temporary marker file, and
    /// ensures the given object-key prefixes exist as directories (created if missing). Never
    /// throws for filesystem problems -- they are reported as unhealthy in the result instead.
    /// </summary>
    /// <param name="requiredPrefixes">Object-key prefixes (e.g. "erp/orders/processed/") to ensure exist</param>
    Task<ObjectStorageHealthResult> CheckHealthAsync(
        IReadOnlyList<string> requiredPrefixes,
        CancellationToken cancellationToken = default);
}
