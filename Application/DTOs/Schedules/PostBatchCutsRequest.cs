// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Shifts;

namespace Klacks.Api.Application.DTOs.Schedules;

public class PostBatchCutsRequest
{
    public List<CutOperation> Operations { get; set; } = new();
}
