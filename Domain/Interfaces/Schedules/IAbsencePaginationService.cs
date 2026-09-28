// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IAbsencePaginationService
{
       Task<TruncatedAbsence> ApplyPaginationAsync(IQueryable<Absence> query, AbsenceFilter filter);
}