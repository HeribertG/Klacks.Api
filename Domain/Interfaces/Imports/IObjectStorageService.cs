// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IObjectStorageService
{
    Task<IReadOnlyList<string>> ListAsync(string prefix, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StorageObjectMetadata>> ListWithMetadataAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the stored object for reading. Throws FileNotFoundException (or DirectoryNotFoundException) when no
    /// object exists under key; the importer maps exactly these two to StoredFileMissing.
    /// </summary>
    Task<Stream> DownloadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves an object only if the source still exists and the destination is still free, and reports
    /// whether this caller won. Implementations must make the check and the move a single atomic step so
    /// concurrent callers -- a second API instance, or an overlapping run -- cannot both move one object.
    /// Used by the inbox registration to give a file an ERP dropped under its own name a unique key, and by
    /// the legacy folder migration to move pre-ledger files back into the inbox. The write timestamp is kept.
    /// </summary>
    /// <param name="sourceKey">Key of the object to move; a key another caller already moved yields false</param>
    /// <param name="destinationKey">Key the object is moved to; an occupied key yields false</param>
    Task<bool> TryMoveAsync(string sourceKey, string destinationKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores the content under key and returns its size, the SHA-256 of the raw bytes, computed
    /// while copying (ErpImportFileHasher) so the stored object never has to be read a second time, and
    /// the stored object's last write time.
    /// </summary>
    Task<StoredObjectInfo> UploadAsync(string key, Stream content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the size, the SHA-256 of the stored object's raw bytes (same hashing path as UploadAsync) and
    /// its last write time (UTC), or null when no object exists under key -- e.g. it vanished after it was listed.
    /// </summary>
    Task<StoredObjectInfo?> GetObjectInfoAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes the object stored under key; a no-op when no object exists there.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an object-key prefix to its absolute on-disk location, for display in diagnostics
    /// and settings answers. Pure path computation -- no file-system access, no directory creation.
    /// Throws for a key that would resolve outside of the storage root.
    /// </summary>
    /// <param name="key">Object-key prefix or file key (e.g. "erp/orders/")</param>
    string ResolvePath(string key);

    /// <summary>
    /// Verifies that the storage root is reachable and writable via a temporary marker file, and
    /// ensures the given object-key prefixes exist as directories (created if missing). Never
    /// throws for filesystem problems -- they are reported as unhealthy in the result instead.
    /// </summary>
    /// <param name="requiredPrefixes">Object-key prefixes (e.g. the drop point's inbox "erp/orders/") to ensure exist</param>
    Task<ObjectStorageHealthResult> CheckHealthAsync(
        IReadOnlyList<string> requiredPrefixes,
        CancellationToken cancellationToken = default);
}
