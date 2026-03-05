// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Macros;

public interface IBreakMacroService
{
    Task ProcessBreakMacroAsync(Break breakEntry, int? paymentInterval = null);
    Task ReprocessAllBreaksAsync(DateOnly startDate, DateOnly endDate, List<Guid>? clientIds = null);
}
