// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Imports;

public record ErpImportTokenListItemDto(
    Guid Id,
    Guid DropPointId,
    string Name,
    string TokenPrefix,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);
