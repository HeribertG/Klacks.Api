// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of testing a TTS provider connection.
/// </summary>
/// <param name="Success">Whether the connection test succeeded</param>
/// <param name="Error">Error message when Success is false</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record TtsTestResult(bool Success, string? Error);
