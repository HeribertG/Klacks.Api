// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The day and times of one Work row together with its client, as the rule evaluators read them for several clients.
/// </summary>

namespace Klacks.Api.Infrastructure.Services.Schedules;

internal sealed record ClientWorkRow(Guid ClientId, DateOnly Date, TimeOnly StartTime, TimeOnly EndTime);
