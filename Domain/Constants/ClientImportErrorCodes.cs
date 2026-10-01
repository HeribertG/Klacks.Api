// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Codes of a rejected import request (HTTP 400/409 with "code"/"errorCode"): file errors from Parse
/// and request errors from Preview/Commit. The UI translates them, so they are stable.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClientImportErrorCodes
{
    public const string FileTooLarge = "file-too-large";
    public const string FileUnsupported = "file-unsupported";
    public const string FileEmpty = "file-empty";
    public const string TooManyRows = "too-many-rows";
    public const string TooManyColumns = "too-many-columns";
    public const string NoHeaderRow = "no-header-row";
    public const string UnpackedSizeExceeded = "unpacked-size-exceeded";
    public const string InvalidRequest = "invalid-request";
    public const string InvalidPolicy = "invalid-policy";
    public const string RowsHaveErrors = "rows-have-errors";
    public const string AlreadyCommitted = "already-committed";
    public const string UnsupportedLanguage = "unsupported-language";
}
