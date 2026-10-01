// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hard limits of the employee import. They protect the small target machines against oversized or
/// crafted files (zip bombs) and bound the work of one preview or commit.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ClientImportLimits
{
    public const long MaxFileBytes = 5L * 1024 * 1024;

    public const long MaxUnpackedBytes = 20L * 1024 * 1024;

    public const int MaxZipEntries = 1000;

    public const int MaxRows = 1000;

    public const int MaxColumns = 60;

    public const int MaxSamples = 3;

    public const int MaxCellLength = 1000;

    public const int HeaderSearchRows = 10;

    public const int MinHeaderCells = 2;

    public const int ContentSampleRows = 50;

    public const long MaxRequestBytes = 20L * 1024 * 1024;

    public const long MaxUploadRequestBytes = MaxFileBytes + (1024 * 1024);

    public const int UploadMemoryBufferBytes = (int)MaxUploadRequestBytes;

    public const long MaxGridChars = MaxFileBytes;

    public const int GeocodingPauseMilliseconds = 500;

    public const int GeocodingQueueCapacity = MaxRows * 2;

    public const int MaxPersonFieldLength = 100;

    public const int MaxCommunicationValueLength = 100;

    public const int MaxFileNameLength = 260;
}
