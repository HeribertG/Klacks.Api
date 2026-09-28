// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

public record PersonalAccessTokenListItemDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime? CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);
