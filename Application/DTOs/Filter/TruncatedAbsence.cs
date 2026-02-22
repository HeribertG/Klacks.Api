// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Filter;

public class TruncatedAbsence : BaseTruncatedResult
{
    public ICollection<Absence> Absences { get; set; } = null!;
}
