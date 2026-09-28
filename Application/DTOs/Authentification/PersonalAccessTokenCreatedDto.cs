// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

public record PersonalAccessTokenCreatedDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime ExpiresAt,
    string Token);
