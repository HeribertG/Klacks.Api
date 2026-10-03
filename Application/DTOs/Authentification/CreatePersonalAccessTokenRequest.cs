// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Authentification;

public class CreatePersonalAccessTokenRequest
{
    public string Name { get; set; } = string.Empty;

    public int? ExpiresInDays { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter<PersonalAccessTokenAccessMode>))]
    public PersonalAccessTokenAccessMode? AccessMode { get; set; }
}
