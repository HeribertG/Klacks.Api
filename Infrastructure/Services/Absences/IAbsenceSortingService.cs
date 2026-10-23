// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Absences;

public interface IAbsenceSortingService
{
    IQueryable<Absence> ApplySorting(IQueryable<Absence> query, string orderBy, string sortOrder, string language);
}