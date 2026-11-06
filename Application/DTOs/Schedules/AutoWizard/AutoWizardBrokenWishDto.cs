// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.AutoWizard;

/// <summary>
/// One schedule-command wish the soft-keyword alternative of an AutoWizard run misses, with the employee's name so the
/// assistant and the UI can name it without a second lookup.
/// </summary>
/// <param name="ClientId">The employee.</param>
/// <param name="ClientName">Display name of the employee; the id as text when the name could not be read.</param>
/// <param name="Date">The day.</param>
/// <param name="Keyword">Keyword name (ScheduleCommandKeyword): the broken restriction or the unfulfilled positive wish.</param>
/// <param name="Outcome">How the wish was missed (KeywordWishOutcome name: BrokenFree, BrokenOnly, BrokenNo, Unfulfilled).</param>
public sealed record AutoWizardBrokenWishDto(Guid ClientId, string ClientName, DateOnly Date, string Keyword, string Outcome);
