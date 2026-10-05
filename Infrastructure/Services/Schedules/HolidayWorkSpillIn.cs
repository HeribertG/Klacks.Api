// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Result of <see cref="HolidayWorkSpillInLoader"/>: per client the range days covered by work of the day before
/// the range, plus the display names of those clients.
/// </summary>
/// <param name="DatesByClient">Client id to the covered days inside the range</param>
/// <param name="ClientNames">Client id to display name</param>
public sealed record HolidayWorkSpillIn(
    IReadOnlyDictionary<Guid, List<DateOnly>> DatesByClient,
    IReadOnlyDictionary<Guid, string> ClientNames)
{
    public static HolidayWorkSpillIn Empty { get; } = new(new Dictionary<Guid, List<DateOnly>>(), new Dictionary<Guid, string>());
}
