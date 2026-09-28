// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Authentification;

public class OAuthClient : BaseEntity
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientName { get; set; } = string.Empty;

    public string RedirectUrisJson { get; set; } = string.Empty;
}
