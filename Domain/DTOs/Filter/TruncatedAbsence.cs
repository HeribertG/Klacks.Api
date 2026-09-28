// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.DTOs.Filter;

public class TruncatedAbsence : BaseTruncatedResult
{
    public ICollection<Absence> Absences { get; set; } = null!;
}
