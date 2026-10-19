// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names of the sub-folders below a drop point's inbox that the import used before the erp_import_file ledger
/// (processing/ while importing, processed/ and error/ afterwards). Only ErpImportLegacyFolderMigrator reads
/// them: it turns every file still found there into a ledger row and moves it back into the inbox. Nothing
/// writes these folders any more.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportLegacyFolders
{
    public const string Processing = "processing";

    public const string Processed = "processed";

    public const string Error = "error";
}
