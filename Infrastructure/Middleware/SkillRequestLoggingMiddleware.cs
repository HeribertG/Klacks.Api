// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Writes the audit line for requests the assistant made on the user's behalf. Routing skill mutations
/// through the own REST API is only worth its round trip if the write is actually attributable, and the
/// framework's request logging records neither headers nor the acting skill. Only requests carrying
/// X-Klacksy-Skill are logged, so ordinary browser traffic produces no extra noise.
/// </summary>
/// <param name="next">The rest of the pipeline</param>
/// <param name="logger">Receives one information line per assistant-caused request</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Infrastructure.Middleware;

public sealed class SkillRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SkillRequestLoggingMiddleware> _logger;

    public SkillRequestLoggingMiddleware(RequestDelegate next, ILogger<SkillRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(SelfApiHeaders.SkillName, out var skillName))
        {
            await _next(context);
            return;
        }

        await _next(context);

        context.Request.Headers.TryGetValue(SelfApiHeaders.CorrelationId, out var correlationId);

        // Distinguishes a write a person triggered from one a background path minted a token for —
        // "the automation did this" versus "someone did this" is the whole point of the audit line.
        var actedFor = context.User.FindFirst(TokenClaimTypes.TokenUse)?.Value == TokenClaimTypes.InternalTokenUse
            ? "background"
            : "user";

        _logger.LogInformation(
            "Assistant request: {Method} {Path} -> {StatusCode} (skill {SkillName}, conversation {CorrelationId}, user {UserId}, origin {Origin})",
            context.Request.Method,
            context.Request.Path.ToString().ForLog(),
            context.Response.StatusCode,
            skillName.ToString().ForLog(),
            correlationId.ToString().ForLog(),
            context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value.ForLog() ?? "anonymous",
            actedFor);
    }
}
