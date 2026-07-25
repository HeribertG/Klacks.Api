// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class IndividualPeriodResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<PeriodResource> Periods { get; set; } = [];
}
