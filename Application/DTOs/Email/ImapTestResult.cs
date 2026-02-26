// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class ImapTestResult
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? ErrorDetails { get; set; }

    public int? MessageCount { get; set; }
}
