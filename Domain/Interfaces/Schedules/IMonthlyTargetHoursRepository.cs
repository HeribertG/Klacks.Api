// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IMonthlyTargetHoursRepository : IBaseRepository<MonthlyTargetHours>
{
    Task<List<MonthlyTargetHours>> ListByYear(int year);

    Task<MonthlyTargetHours?> GetByYearMonth(int year, int month);
}
