// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Imports;

public record StorageObjectMetadata(string Key, long SizeBytes, DateTime LastModifiedUtc);
