// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.KeywordWishes;

/// <summary>
/// Schedule-command wishes the final plan misses, counted from the persisted works against the employees' commands.
/// </summary>
/// <param name="BrokenFree">Worked days the employee marked FREE (also a combination that leaves no shift kind).</param>
/// <param name="BrokenOnly">Worked days of another kind than the employee's EARLY / LATE / NIGHT.</param>
/// <param name="BrokenNo">Worked days of a kind the employee excluded with -EARLY / -LATE / -NIGHT.</param>
/// <param name="Unfulfilled">Positive wishes (-FREE, EARLY / LATE / NIGHT) without a matching shift.</param>
/// <param name="Items">The single missed wishes, ordered by employee and day.</param>
public sealed record KeywordWishesDto(
    int BrokenFree,
    int BrokenOnly,
    int BrokenNo,
    int Unfulfilled,
    IReadOnlyList<KeywordWishItemDto> Items)
{
    public static KeywordWishesDto Empty { get; } = new(0, 0, 0, 0, []);
}
