// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single place that lifts the caller's bearer token off an incoming request so it can ride along to
/// the skills. Reading the header here rather than through IHttpContextAccessor deeper in the stack is
/// deliberate: the assistant runs work in background tasks, where the accessor's AsyncLocal is racy.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Presentation.Extensions;

public static class HttpRequestExtensions
{
    /// <param name="request">The incoming HTTP request</param>
    public static BearerToken? GetBearerToken(this HttpRequest request) =>
        BearerToken.FromAuthorizationHeader(request.Headers.Authorization.ToString());
}
