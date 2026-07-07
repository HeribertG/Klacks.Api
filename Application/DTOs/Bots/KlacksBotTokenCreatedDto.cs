// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Bots;

public record KlacksBotTokenCreatedDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime ExpiresAt,
    string Token);
