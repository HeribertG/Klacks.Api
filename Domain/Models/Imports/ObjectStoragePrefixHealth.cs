// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Imports;

public record ObjectStoragePrefixHealth(string Prefix, bool Ready, string? Error);
