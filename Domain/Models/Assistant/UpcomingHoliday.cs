// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// A public holiday that falls today or tomorrow, used to add a warm, scheduling-relevant note to
/// the welcome greeting.
/// </summary>
/// <param name="Name">Localized holiday name (as provided by the source, e.g. "Auffahrt").</param>
/// <param name="IsToday">True when the holiday is today; false when it is tomorrow.</param>
public sealed record UpcomingHoliday(string Name, bool IsToday);
