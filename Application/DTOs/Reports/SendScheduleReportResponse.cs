// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class SendScheduleReportResponse
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ClientEmail { get; set; }
}
