// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Shifts;

namespace Klacks.Api.Application.DTOs.Schedules;

public class PostBatchCutsRequest
{
    public List<CutOperation> Operations { get; set; } = new();
}
