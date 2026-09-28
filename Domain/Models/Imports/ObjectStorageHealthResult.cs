// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

public record ObjectStorageHealthResult(
    string RootPath,
    bool RootDirectoryExisted,
    bool RootDirectoryReady,
    bool IsWritable,
    string? WriteTestError,
    IReadOnlyList<ObjectStoragePrefixHealth> Prefixes);
