// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class PostResetCutsRequest
{
    public Guid OriginalId { get; set; }

    public DateTime NewStartDate { get; set; }
}
