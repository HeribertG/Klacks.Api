// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Reports;

public class SendScheduleReportResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientEmail { get; set; }
}
