// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public class AbsenceDetailResource
{
    public Guid Id { get; set; }

    public Guid AbsenceId { get; set; }

    public AbsenceDetailMode Mode { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal Duration { get; set; }

    public MultiLanguage DetailName { get; set; } = null!;

    public MultiLanguage? Description { get; set; }
}
