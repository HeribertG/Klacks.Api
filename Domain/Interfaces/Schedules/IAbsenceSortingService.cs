// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IAbsenceSortingService
{
    IQueryable<Absence> ApplySorting(IQueryable<Absence> query, string orderBy, string sortOrder, string language);
}