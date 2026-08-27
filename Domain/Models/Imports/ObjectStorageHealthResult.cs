// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Imports;

public record ObjectStorageHealthResult(
    string RootPath,
    bool RootDirectoryExisted,
    bool RootDirectoryReady,
    bool IsWritable,
    string? WriteTestError,
    IReadOnlyList<ObjectStoragePrefixHealth> Prefixes);
