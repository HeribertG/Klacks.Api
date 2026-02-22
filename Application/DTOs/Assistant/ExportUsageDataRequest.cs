// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public class ExportUsageDataRequest
{
    public string Format { get; set; } = "csv";

    public int Days { get; set; } = 30;

    public string? UserId { get; set; }
}