// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Settings;

public class EmailTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageKey { get; set; }
    public Dictionary<string, string>? MessageParams { get; set; }
    public string? ErrorDetails { get; set; }
}