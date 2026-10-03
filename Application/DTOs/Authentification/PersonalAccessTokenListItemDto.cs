// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Authentification;

public record PersonalAccessTokenListItemDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime? CreatedAt,
    DateTime? ExpiresAt,
    DateTime? LastUsedAt,
    [property: JsonConverter(typeof(JsonStringEnumConverter<PersonalAccessTokenAccessMode>))]
    PersonalAccessTokenAccessMode AccessMode);
