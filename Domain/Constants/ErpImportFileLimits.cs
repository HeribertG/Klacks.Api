// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Column lengths of the erp_import_file ledger. Shared by the entity's MaxLength attributes and by the
/// code that has to fit a value into a column (failure reason clamping, instance id length), plus the
/// largest stored file the importer reads into memory (the same limit the upload endpoints enforce).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportFileLimits
{
    public const long MaxFileSizeBytes = ErpOrderUploadConstants.MaxFileSizeBytes;

    public const int SourceSystemIdMaxLength = 200;

    public const int StorageKeyMaxLength = 1000;

    public const int FileNameMaxLength = 500;

    public const int Sha256HexLength = 64;

    public const int UploadedByMaxLength = 200;

    public const int ClaimedByMaxLength = 100;

    public const int FailureReasonMaxLength = 2000;
}
