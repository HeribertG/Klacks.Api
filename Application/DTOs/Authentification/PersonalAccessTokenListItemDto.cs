// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Authentification;

public record PersonalAccessTokenListItemDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime? CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);
