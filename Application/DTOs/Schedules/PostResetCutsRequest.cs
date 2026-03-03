// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class PostResetCutsRequest
{
    public Guid OriginalId { get; set; }

    public DateTime NewStartDate { get; set; }
}
