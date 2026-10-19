// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks that the stored object of an erp_import_file row still exists and still has the SHA-256 recorded
/// on receipt -- strict, the same rule for the importer and the retry. VerifyAsync (retry, before re-queueing)
/// only hashes the object and returns null when it is intact, otherwise StoredFileMissing or
/// FileChangedAfterReceipt. ReadVerifiedAsync (importer) reads the object exactly ONCE: the bytes are buffered,
/// hashed while buffering (ErpImportFileHasher) and handed back for parsing only when they match the row, so the
/// bytes that are imported are exactly the bytes that were verified. The buffer is bounded by the recorded size
/// (more bytes = changed) and the row is refused with FileTooLarge before reading when its recorded size exceeds
/// ErpImportFileLimits.MaxFileSizeBytes. Only FileNotFoundException/DirectoryNotFoundException count as missing;
/// any other read error propagates (the importer finishes the attempt as Interrupted).
/// </summary>
/// <param name="objectStorageService">Hashes or downloads the stored object</param>
/// <param name="file">Ledger row whose storage key, recorded size and recorded hash are checked</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;

namespace Klacks.Api.Application.Services.Imports;

public static class ErpImportStoredFileVerifier
{
    public static async Task<string?> VerifyAsync(IObjectStorageService objectStorageService, ErpImportFile file, CancellationToken cancellationToken)
    {
        var storedObject = await objectStorageService.GetObjectInfoAsync(file.StorageKey, cancellationToken);
        if (storedObject == null)
        {
            return ErpImportFileFailureReasons.StoredFileMissing;
        }

        return string.Equals(storedObject.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase)
            ? null
            : ErpImportFileFailureReasons.FileChangedAfterReceipt;
    }

    public static async Task<ErpImportStoredFileContent> ReadVerifiedAsync(IObjectStorageService objectStorageService, ErpImportFile file, CancellationToken cancellationToken)
    {
        if (file.SizeBytes > ErpImportFileLimits.MaxFileSizeBytes)
        {
            return ErpImportStoredFileContent.Failed(ErpImportFileFailureReasons.FileTooLarge);
        }

        Stream stream;
        try
        {
            stream = await objectStorageService.DownloadAsync(file.StorageKey, cancellationToken);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return ErpImportStoredFileContent.Failed(ErpImportFileFailureReasons.StoredFileMissing);
        }

        await using (stream)
        {
            using var buffer = new MemoryStream();
            var storedObject = await ErpImportFileHasher.CopyAndHashAsync(stream, buffer, file.SizeBytes, cancellationToken);

            return storedObject != null && string.Equals(storedObject.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase)
                ? ErpImportStoredFileContent.Verified(buffer.ToArray())
                : ErpImportStoredFileContent.Failed(ErpImportFileFailureReasons.FileChangedAfterReceipt);
        }
    }
}
