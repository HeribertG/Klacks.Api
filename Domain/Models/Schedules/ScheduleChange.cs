// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class ScheduleChange : BaseEntity
{
    public Guid ClientId { get; set; }
    public DateOnly ChangeDate { get; set; }
}
