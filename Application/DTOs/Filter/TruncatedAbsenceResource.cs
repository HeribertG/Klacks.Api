// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

using Klacks.Api.Domain.DTOs.Filter;
namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedAbsenceResource : BaseTruncatedResult
{
    public ICollection<AbsenceResource> Absences { get; set; } = null!;
}
