// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partial class for IndividualPeriod and Period mappings.
/// </summary>
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

public partial class ScheduleMapper
{
    public partial PeriodResource ToPeriodResource(Period period);

    public partial IndividualPeriodResource ToIndividualPeriodResource(IndividualPeriod individualPeriod);

    [MapperIgnoreTarget(nameof(Period.CreateTime))]
    [MapperIgnoreTarget(nameof(Period.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Period.UpdateTime))]
    [MapperIgnoreTarget(nameof(Period.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Period.DeletedTime))]
    [MapperIgnoreTarget(nameof(Period.IsDeleted))]
    [MapperIgnoreTarget(nameof(Period.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Period.IndividualPeriod))]
    public partial Period ToPeriodEntity(PeriodResource resource);

    [MapperIgnoreTarget(nameof(IndividualPeriod.CreateTime))]
    [MapperIgnoreTarget(nameof(IndividualPeriod.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(IndividualPeriod.UpdateTime))]
    [MapperIgnoreTarget(nameof(IndividualPeriod.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(IndividualPeriod.DeletedTime))]
    [MapperIgnoreTarget(nameof(IndividualPeriod.IsDeleted))]
    [MapperIgnoreTarget(nameof(IndividualPeriod.CurrentUserDeleted))]
    public partial IndividualPeriod ToIndividualPeriodEntity(IndividualPeriodResource resource);
}
