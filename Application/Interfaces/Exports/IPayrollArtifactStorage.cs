// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Storage of the generated payroll export files for re-download, kept apart from the ERP drop-zone storage.
/// A key is a slash-separated relative path; a key that would leave the storage root is rejected.
/// @param key - Storage key of the artifact
/// @param content - Bytes of the generated file
/// </summary>
namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollArtifactStorage
{
    Task UploadAsync(string key, byte[] content, CancellationToken cancellationToken = default);

    /// <summary>Returns the stored bytes, or null when no artifact exists under the key.</summary>
    Task<byte[]?> ReadAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}