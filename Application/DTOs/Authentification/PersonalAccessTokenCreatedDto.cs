// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Authentification;

public record PersonalAccessTokenCreatedDto(
    Guid Id,
    string Name,
    string TokenPrefix,
    DateTime ExpiresAt,
    string Token,
    [property: JsonConverter(typeof(JsonStringEnumConverter<PersonalAccessTokenAccessMode>))]
    PersonalAccessTokenAccessMode AccessMode);
