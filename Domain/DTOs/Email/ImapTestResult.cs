// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.Email;

public class ImapTestResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? MessageKey { get; set; }

    public Dictionary<string, string>? MessageParams { get; set; }

    public string? ErrorDetails { get; set; }

    public int? MessageCount { get; set; }
}
