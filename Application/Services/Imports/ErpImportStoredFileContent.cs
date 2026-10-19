// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Imports;

/// <summary>
/// Result of reading a stored ERP import file once: either its verified bytes or the failure reason.
/// </summary>
/// <param name="Content">Raw bytes whose size and SHA-256 match the ledger row; null on failure</param>
/// <param name="FailureReason">StoredFileMissing, FileChangedAfterReceipt or FileTooLarge; null on success</param>
public sealed record ErpImportStoredFileContent(byte[]? Content, string? FailureReason)
{
    public static ErpImportStoredFileContent Verified(byte[] content) => new(content, null);

    public static ErpImportStoredFileContent Failed(string failureReason) => new(null, failureReason);
}
