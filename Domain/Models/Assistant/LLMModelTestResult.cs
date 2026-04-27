// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Result of a completion test performed on a single model during sync.
/// </summary>
/// <param name="ApiModelId">Provider-side model identifier used in the test request</param>
/// <param name="ModelName">Human-readable display name of the model</param>
/// <param name="Passed">True when the model responded successfully within the timeout</param>
/// <param name="ErrorMessage">Failure reason; null when Passed is true</param>
/// <param name="DurationMs">Wall-clock milliseconds the test took</param>
public record LLMModelTestResult(
    string ApiModelId,
    string ModelName,
    bool Passed,
    string? ErrorMessage,
    int DurationMs
);
