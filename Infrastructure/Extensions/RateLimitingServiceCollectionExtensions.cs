// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the named rate-limit policies the controllers refer to with [EnableRateLimiting] (login, upload, refresh
/// token, password reset and change, LLM chat, connection test, client import, geo proxy, Playground status). The MCP
/// policy is registered by AddKlacksMcpServer. Rejected requests are answered with 429.
/// </summary>
/// <param name="services">Service collection of the host</param>

using System.Threading.RateLimiting;
using Klacks.Api.Application.Constants;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Infrastructure.Extensions;

public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddKlacksRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitingPolicies.Login, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimitingPolicies.AnonymousPartitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.LoginPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.Upload, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimitingPolicies.AnonymousPartitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.UploadPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.RefreshToken, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimitingPolicies.AnonymousPartitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.RefreshTokenPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.PasswordReset, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimitingPolicies.AnonymousPartitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.PasswordResetPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.PasswordChange, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ForUserOrVisitor(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.PasswordChangePermitLimit,
                        Window = RateLimitingPolicies.PasswordChangeWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.LlmChat, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ForUserOrVisitor(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.LlmChatPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.PlaygroundStatus, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ForNetworkAddress(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.PlaygroundStatusPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.BotQuery, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? RateLimitingPolicies.AnonymousPartitionKey,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.BotQueryPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.ConnectionTest, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ForUserOrVisitor(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.ConnectionTestPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.ClientImport, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ForUserOrVisitor(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.ClientImportPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));

            options.AddPolicy(RateLimitingPolicies.GeoProxy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: RateLimitPartitionKeys.ForUserOrVisitor(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = RateLimitingPolicies.GeoProxyPermitLimit,
                        Window = RateLimitingPolicies.DefaultWindow
                    }));
        });

        return services;
    }
}
