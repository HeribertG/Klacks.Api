// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partial class for MonthlyTargetHours mappings.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

public partial class ScheduleMapper
{
    public partial MonthlyTargetHoursResource ToMonthlyTargetHoursResource(MonthlyTargetHours monthlyTargetHours);

    [MapperIgnoreTarget(nameof(MonthlyTargetHours.CreateTime))]
    [MapperIgnoreTarget(nameof(MonthlyTargetHours.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(MonthlyTargetHours.UpdateTime))]
    [MapperIgnoreTarget(nameof(MonthlyTargetHours.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(MonthlyTargetHours.DeletedTime))]
    [MapperIgnoreTarget(nameof(MonthlyTargetHours.IsDeleted))]
    [MapperIgnoreTarget(nameof(MonthlyTargetHours.CurrentUserDeleted))]
    public partial MonthlyTargetHours ToMonthlyTargetHoursEntity(MonthlyTargetHoursResource resource);
}
