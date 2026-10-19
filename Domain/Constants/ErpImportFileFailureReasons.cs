// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Fixed failure reasons of the ERP import file ledger (as opposed to the parser messages stored for an
/// unparseable file). StaleHeartbeat is stored when the reaper flips an Importing row whose heartbeat stopped;
/// FileChangedAfterReceipt when the stored object's SHA-256 no longer matches the hash recorded on receipt;
/// StoredFileMissing when no object exists under the row's storage key any more (e.g. a row registered by
/// an upload whose file the pre-ledger runner moved out of the inbox); ImportAbortedPrefix, followed by the
/// error message, when the importing process hit an unexpected error and finished the row as Interrupted;
/// LegacyImportInterrupted and LegacyErrorWithoutReason when ErpImportLegacyFolderMigrator turns a file of the
/// pre-ledger processing/ (claimed, never finished) or error/ folder (no recorded exception) into a ledger row.
/// LedgerEntryMissing when the inbox registration finds a file whose name already carries the "{32 hex}-" id
/// Klacks gives every registered file but no row exists for it (e.g. after a database restore or reset): such a
/// file is never imported automatically, an admin checks it and retries or deletes it. FileTooLarge when the
/// stored file is larger than ErpImportFileLimits.MaxFileSizeBytes, the most the importer reads into memory.
/// ConcurrentOpenDraft is the rejection reason of a single order whose insert lost against a second import that
/// created the open draft for the same order reference first (unique index on open drafts).
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ErpImportFileFailureReasons
{
    public const string StaleHeartbeat = "Import interrupted: no progress was reported within the stale threshold.";

    public const string FileChangedAfterReceipt = "File changed after receipt.";

    public const string StoredFileMissing = "The stored file no longer exists.";

    public const string ImportAbortedPrefix = "Import aborted by an unexpected error: ";

    public const string LegacyImportInterrupted = "The import before the update claimed this file and did not finish it.";

    public const string LegacyErrorWithoutReason = "The import before the update rejected this file without a recorded reason.";

    public const string LedgerEntryMissing = "Stored file without its import record, e.g. after a database restore. Check the content, then retry or delete it.";

    public const string FileTooLarge = "The file is larger than the maximum size an import can read.";

    public const string ConcurrentOpenDraft = "Another import created the open draft for this order at the same time. Re-deliver or retry the file to apply it.";
}
