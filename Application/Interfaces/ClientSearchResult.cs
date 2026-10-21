// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public record ClientSearchResult
{
    public required IReadOnlyList<ClientSearchItem> Items { get; init; }
    public int TotalCount { get; init; }
}
