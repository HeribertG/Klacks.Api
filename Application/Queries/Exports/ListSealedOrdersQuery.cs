// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to list sealed orders (Shifts with Status=SealedOrder) that can be exported.
/// @param FromDate - Optional lower bound for Shift.FromDate
/// @param UntilDate - Optional upper bound for Shift.FromDate
/// @param CustomerId - Optional filter for Shift.ClientId (only customer-typed clients)
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record ListSealedOrdersQuery(
    DateOnly? FromDate,
    DateOnly? UntilDate,
    Guid? CustomerId) : IRequest<List<SealedOrderListItem>>;
