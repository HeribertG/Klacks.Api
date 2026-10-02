// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Logging;
using Microsoft.Extensions.Http.Logging;

namespace Klacks.Api.Infrastructure.Http;

/// <summary>
/// Replaces the default IHttpClientFactory request logging for every HttpClient of the host, plugins
/// included. The framework logger only hides the query string; a credential carried in the path (the
/// Telegram bot token in "/bot&lt;token&gt;/") reached the log at Information level. Every URI and error text
/// written here goes through LoggedUriRedactor first. Nothing is logged above Information, like the
/// framework logger, so production (Default=Warning) stays as quiet as before.
/// </summary>
/// <param name="logger">Logger the redacted request lines are written to</param>
public sealed class RedactingHttpClientLogger : IHttpClientLogger
{
    private readonly ILogger<RedactingHttpClientLogger> _logger;

    public RedactingHttpClientLogger(ILogger<RedactingHttpClientLogger> logger)
    {
        _logger = logger;
    }

    public object? LogRequestStart(HttpRequestMessage request)
    {
        _logger.LogDebug("Sending HTTP request {Method} {Uri}", request.Method, request.RequestUri.RedactUriForLog());
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        _logger.LogInformation(
            "HTTP request {Method} {Uri} answered {StatusCode} after {ElapsedMilliseconds}ms",
            request.Method,
            request.RequestUri.RedactUriForLog(),
            (int)response.StatusCode,
            elapsed.TotalMilliseconds);
    }

    public void LogRequestFailed(
        object? context,
        HttpRequestMessage request,
        HttpResponseMessage? response,
        Exception exception,
        TimeSpan elapsed)
    {
        _logger.LogInformation(
            "HTTP request {Method} {Uri} failed after {ElapsedMilliseconds}ms: {ExceptionType} {Error}",
            request.Method,
            request.RequestUri.RedactUriForLog(),
            elapsed.TotalMilliseconds,
            exception.GetType().Name,
            exception.Message.RedactUriForLog());
    }
}
