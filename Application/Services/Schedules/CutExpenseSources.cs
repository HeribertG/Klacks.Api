// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Where the new pieces of one cut batch take their default expenses from, beyond the stored rows: the plannable shift each
/// sealed order converts in this batch, and the rows of the pieces created so far (not saved yet, so not readable from the
/// database).
/// </summary>
/// <param name="PlannableByOrder">Sealed order id to the id of its plannable shift that this batch converts into a part</param>
internal sealed record CutExpenseSources(IReadOnlyDictionary<Guid, Guid> PlannableByOrder)
{
    public Dictionary<Guid, List<ShiftExpenses>> OfCreated { get; } = new();
}
