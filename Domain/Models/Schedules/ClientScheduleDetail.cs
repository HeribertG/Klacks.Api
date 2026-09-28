// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

public class ClientScheduleDetail
{
    public Guid ClientId { get; set; }

    public int CurrentMonth { get; set; }

    public int CurrentYear { get; set; }

    public Guid Id { get; set; }

    public int NeededRows { get; set; } = 3;
}
