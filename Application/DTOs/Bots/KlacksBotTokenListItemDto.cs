// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Bots;

public record KlacksBotTokenListItemDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt);
