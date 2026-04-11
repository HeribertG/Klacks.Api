// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of testing an STT provider connection.
/// </summary>
/// <param name="Success">Whether the connection test succeeded</param>
/// <param name="Error">Error message when Success is false</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record SttTestResult(bool Success, string? Error);
