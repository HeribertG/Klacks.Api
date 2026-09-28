// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Imports;

public record ObjectStoragePrefixHealth(string Prefix, bool Ready, string? Error);
