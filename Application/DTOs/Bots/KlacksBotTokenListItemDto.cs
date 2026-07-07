// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Bots;

public record KlacksBotTokenListItemDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);
