// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service fuer die automatische Befuellung von Container-Templates mit passenden Shifts.
/// Loest ein Orienteering Problem: Waehle Teilmenge von Shifts innerhalb des Zeitbudgets und optimiere die Route.
/// </summary>
/// <param name="request">Enthaelt Container-ID, Zeitfenster, Basis-Adressen und Transportmodus</param>

using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IContainerAutofillService
{
    Task<ContainerAutofillResult> AutofillAsync(ContainerAutofillRequest request);
}
