// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Application.DTOs.Clients;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedClientResource : BaseTruncatedResult
{
    public ICollection<ClientListItemResource>? Clients { get; set; }

    public string Editor { get; set; } = string.Empty;

    public DateTime LastChange { get; set; }
}
