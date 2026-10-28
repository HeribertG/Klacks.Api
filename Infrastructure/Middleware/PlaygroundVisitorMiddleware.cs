// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Publishes the Playground visitor (normalized IP bucket and validated device id) as ambient context for the
/// rest of the request, so budgeted LLM calls anywhere below it, including detached tasks, can charge the right
/// buckets. Must run after the forwarded-headers middleware so the remote address is the real client address.
/// The device token comes from the X-Playground-Device header only.
/// With the Playground mode off it forwards the request untouched: no header is read and nothing is validated.
/// </summary>
/// <param name="next">The rest of the pipeline</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Playground;
using Klacks.Api.Domain.Services.Playground;

namespace Klacks.Api.Infrastructure.Middleware;

public sealed class PlaygroundVisitorMiddleware
{
    private readonly RequestDelegate _next;

    public PlaygroundVisitorMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPlaygroundMode mode, IPlaygroundDeviceTokenService tokens)
    {
        if (!mode.IsActive)
        {
            await _next(context);
            return;
        }

        var deviceId = tokens.TryGetDeviceId(context.Request.Headers[PlaygroundTokenLimitConstants.DeviceHeaderName].FirstOrDefault());
        var ipKey = PlaygroundIpNormalizer.Normalize(context.Connection.RemoteIpAddress);
        PlaygroundVisitorContext.Set(new PlaygroundVisitor(ipKey, deviceId));

        try
        {
            await _next(context);
        }
        finally
        {
            PlaygroundVisitorContext.Clear();
        }
    }
}
