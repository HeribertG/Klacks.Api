// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class ExportUsageDataRequest
{
    public string Format { get; set; } = "csv";

    public int Days { get; set; } = 30;

    public string? UserId { get; set; }
}