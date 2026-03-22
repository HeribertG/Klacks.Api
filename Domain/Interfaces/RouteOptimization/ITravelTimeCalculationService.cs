// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service for calculating travel times between work locations.
/// </summary>
/// <param name="from">Origin address</param>
/// <param name="to">Destination address</param>
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface ITravelTimeCalculationService
{
    Task<bool> IsApiKeyConfiguredAsync();
    Task<TimeSpan?> CalculateTravelTimeAsync(Address from, Address to, CancellationToken ct);
}
