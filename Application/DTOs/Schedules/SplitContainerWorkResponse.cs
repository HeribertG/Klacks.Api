// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class SplitContainerWorkResponse
{
    public WorkResource? FirstPart { get; set; }

    public WorkResource? SecondPart { get; set; }
}
