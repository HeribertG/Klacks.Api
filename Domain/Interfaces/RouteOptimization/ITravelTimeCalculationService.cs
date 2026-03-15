// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service zur Berechnung von Reisezeiten zwischen Einsatzorten.
/// </summary>
/// <param name="from">Ausgangsadresse</param>
/// <param name="to">Zieladresse</param>
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface ITravelTimeCalculationService
{
    Task<bool> IsApiKeyConfiguredAsync();
    Task<TimeSpan?> CalculateTravelTimeAsync(Address from, Address to, CancellationToken ct);
}
