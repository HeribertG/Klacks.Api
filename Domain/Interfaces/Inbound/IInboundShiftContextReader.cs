// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundShiftContextReader
{
    Task<IReadOnlyList<ClarificationShift>> GetShiftsAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, int maxCount, CancellationToken cancellationToken = default);
}
