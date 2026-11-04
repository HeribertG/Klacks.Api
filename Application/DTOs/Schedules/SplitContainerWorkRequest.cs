// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class SplitContainerWorkRequest
{
    public TimeOnly SplitTime { get; set; }

    public Guid TargetClientId { get; set; }
}
