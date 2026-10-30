// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The result of resolving a split_shifts_by_weekday call without writing anything: the group, the plannable shifts that
/// would be cut and the ones that are skipped with their reason, or the refusal when the group cannot be resolved.
/// </summary>
/// <param name="GroupId">The resolved group, empty when the call is refused</param>
/// <param name="GroupName">Name of the resolved group, empty when the call is refused</param>
/// <param name="ToCut">The plannable shifts that run on at least two weekdays and are still uncut</param>
/// <param name="Skipped">The plannable shifts the cut leaves alone</param>
/// <param name="Refusal">Why the call cannot be resolved, null when it can</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ShiftWeekdayCutSelection(
    Guid GroupId,
    string GroupName,
    IReadOnlyList<Shift> ToCut,
    IReadOnlyList<ShiftWeekdayCutSkip> Skipped,
    string? Refusal)
{
    public bool IsRefused => Refusal is not null;

    public static ShiftWeekdayCutSelection Refuse(string refusal) => new(Guid.Empty, string.Empty, [], [], refusal);
}
