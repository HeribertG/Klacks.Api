// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class ResetDateRangeResponse
{
    public DateOnly EarliestResetDate { get; set; }

    public DateOnly? UntilDate { get; set; }
}
