// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exception thrown when a provider returns a non-success HTTP status for a chat/completions
/// request. Subclasses InvalidOperationException so existing catch sites and message-based
/// detection (e.g. the DeepSeek tool_choice fallback) keep working, while carrying the status
/// code and classification flags so callers can suppress redundant stack-trace logging for
/// expected model-incompatibilities and transient upstream failures.
/// </summary>
/// <param name="message">Provider error message in the "{Provider} API error for model {Model}: ..." format, so a failing model is identifiable from the message alone</param>
/// <param name="statusCode">HTTP status code returned by the provider</param>
/// <param name="isExpectedModelIncompatibility">True when the body identifies a non-chat model probed against chat/completions</param>
/// <param name="isTransient">True when the status code indicates a retryable upstream failure (5xx or 429)</param>

using System.Net;

namespace Klacks.Api.Infrastructure.Services.Assistant.Providers.Base;

public sealed class LLMProviderHttpException(
    string message,
    HttpStatusCode statusCode,
    bool isExpectedModelIncompatibility,
    bool isTransient) : InvalidOperationException(message)
{
    public HttpStatusCode StatusCode { get; } = statusCode;

    public bool IsExpectedModelIncompatibility { get; } = isExpectedModelIncompatibility;

    public bool IsTransient { get; } = isTransient;

    public bool IsExpected => IsExpectedModelIncompatibility || IsTransient;
}
