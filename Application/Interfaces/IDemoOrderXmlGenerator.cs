// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Renders the demo shift plan as an ERP order import document, so a fresh install that seeds demo
/// clients without demo shifts can obtain the same plan through the regular ERP import instead.
/// </summary>

using Klacks.Api.Application.DTOs.Seed;

namespace Klacks.Api.Application.Interfaces;

public interface IDemoOrderXmlGenerator
{
    /// <summary>
    /// Builds the order import document.
    /// </summary>
    /// <param name="customers">Seeded customers the orders are distributed over; must not be empty</param>
    /// <param name="language">Two-letter language code selecting order names and descriptions</param>
    string Generate(IReadOnlyList<DemoOrderCustomer> customers, string language);
}
