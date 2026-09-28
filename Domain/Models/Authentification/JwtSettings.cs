// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Authentification;

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;

    public string ValidAudience { get; set; } = string.Empty;

    public string ValidIssuer { get; set; } = string.Empty;
}
