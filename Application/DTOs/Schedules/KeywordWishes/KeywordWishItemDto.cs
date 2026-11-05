// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.KeywordWishes;

/// <summary>
/// One missed schedule-command wish of the final plan.
/// </summary>
/// <param name="ClientId">The employee.</param>
/// <param name="Date">The day.</param>
/// <param name="Keyword">Keyword name (ScheduleCommandKeyword): the combined restriction that was broken, or the unfulfilled positive wish.</param>
/// <param name="Outcome">How the wish was missed (KeywordWishOutcome name: BrokenFree, BrokenOnly, BrokenNo, Unfulfilled).</param>
public sealed record KeywordWishItemDto(Guid ClientId, DateOnly Date, string Keyword, string Outcome);
