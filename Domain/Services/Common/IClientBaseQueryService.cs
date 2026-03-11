// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Zentraler Service für Client-Base-Queries mit Membership-, Gruppen-, Such- und Typ-Filter.
/// Gibt IQueryable zurück damit nachfolgende Operationen (Includes, Count, Paging) in der DB bleiben.
/// </summary>
/// <param name="filter">Gemeinsamer Filter mit Zeitraum, Suche, Gruppe und Sortierung</param>
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Common;

public interface IClientBaseQueryService
{
    Task<IQueryable<Client>> BuildBaseQuery(ClientBaseFilter filter);
}
