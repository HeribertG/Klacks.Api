// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to load the detail preview of a single sealed order including all work entries
/// in the optional date range with their lock level and period-closed flag.
/// @param Id - Identifier of the sealed order shift (Status = SealedOrder)
/// @param FromDate - Optional lower bound for Work.CurrentDate
/// @param UntilDate - Optional upper bound for Work.CurrentDate
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record GetSealedOrderDetailsQuery(
    Guid Id,
    DateOnly? FromDate,
    DateOnly? UntilDate) : IRequest<SealedOrderDetailsResource?>;
