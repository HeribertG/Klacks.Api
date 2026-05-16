// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to list sealed orders (Shifts with Status=SealedOrder) that can be exported.
/// @param FromDate - Optional lower bound for Shift active window
/// @param UntilDate - Optional upper bound for Shift active window
/// @param CustomerId - Optional filter for Shift.ClientId (only customer-typed clients)
/// @param SearchTerm - Optional case-insensitive search across Abbreviation, Name, Customer.Name, Customer.FirstName, Customer.IdNumber
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record ListSealedOrdersQuery(
    DateOnly? FromDate,
    DateOnly? UntilDate,
    Guid? CustomerId,
    string? SearchTerm) : IRequest<List<SealedOrderListItem>>;
