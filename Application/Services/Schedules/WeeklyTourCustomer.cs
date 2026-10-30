// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A customer of the planned tours, named in the answer.
/// </summary>
/// <param name="ClientId">Id of the customer; null for a service without a customer</param>
/// <param name="Name">Display name of the customer, or the service name when there is no customer</param>
/// <param name="Reason">Why the customer is not planned; empty for a planned one</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record WeeklyTourCustomer(Guid? ClientId, string Name, string Reason);
