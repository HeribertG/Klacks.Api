// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Archives;

public enum ZipArchiveLimitViolation
{
    None,
    TooManyEntries,
    UnpackedSizeExceeded,
    CompressionRatioExceeded,
    Unreadable
}
