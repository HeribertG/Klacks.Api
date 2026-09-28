// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Imports;

public record ErpImportTokenListItemDto(
    Guid Id,
    Guid DropPointId,
    string Name,
    string TokenPrefix,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);
