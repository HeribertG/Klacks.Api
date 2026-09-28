// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

public class CreatePersonalAccessTokenRequest
{
    public string Name { get; set; } = string.Empty;

    public int? ExpiresInDays { get; set; }
}
