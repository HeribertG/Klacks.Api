// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public record ShiftSearchResult
{
    public required IReadOnlyList<ShiftSearchItem> Items { get; init; }
    public int TotalCount { get; init; }
}
