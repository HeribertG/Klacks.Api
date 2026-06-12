// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Authentification;

public record PersonalAccessTokenCreatedDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime ExpiresAt,
    string Token);
