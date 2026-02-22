// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IAbsenceRepository : IBaseRepository<Absence>
{
    HttpResultResource CreateExcelFile(string language);

    Task<TruncatedAbsence> Truncated(AbsenceFilter filter);
}
