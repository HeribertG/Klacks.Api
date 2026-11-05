// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Defines policy names and limits for ASP.NET Rate Limiting middleware.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class RateLimitingPolicies
{
    public const string Login = "login";
    public const string Upload = "upload";
    public const string RefreshToken = "refresh-token";
    public const string PasswordReset = "password-reset";
    public const string PasswordChange = "password-change";
    public const string LlmChat = "llm-chat";
    public const string Mcp = "mcp";
    public const string BotQuery = "bot-query";
    public const string ConnectionTest = "connection-test";
    public const string ClientImport = "client-import";
    public const string GeoProxy = "geo-proxy";
    public const string PlaygroundStatus = "playground-status";

    public const int LoginPermitLimit = 20;
    public const int UploadPermitLimit = 30;
    /// <summary>
    /// Refresh requests per IP and window. Raised from 30 to 120 with the refresh token cookie: after the rollout every
    /// open tab performs a one-time migration refresh on its next load, and an office behind one NAT address reaches a
    /// cloud instance as a single IP, so a reload wave of more than 30 tabs within a minute would otherwise answer 429.
    /// Guessing a 256-bit refresh token is not feasible at any rate; the limit only caps load.
    /// </summary>
    public const int RefreshTokenPermitLimit = 120;
    public const int PasswordResetPermitLimit = 5;
    public const int PasswordChangePermitLimit = 5;
    public const int LlmChatPermitLimit = 30;
    public const int McpPermitLimit = 60;
    public const int BotQueryPermitLimit = 20;
    public const int ConnectionTestPermitLimit = 10;
    public const int ClientImportPermitLimit = 120;
    public const int GeoProxyPermitLimit = 20;
    public const int PlaygroundStatusPermitLimit = 120;

    public const string AnonymousPartitionKey = "anonymous";
    public const string PlaygroundPartitionPrefix = "playground:";
    public const string PlaygroundNoDeviceKey = "no-device";
    public const char PlaygroundPartitionSeparator = '|';
    public const int MaxBulkOperationItems = 500;
    public const int MaxFunctionBatchSize = 20;

    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(1);
    public static readonly TimeSpan PasswordChangeWindow = TimeSpan.FromMinutes(15);
}
