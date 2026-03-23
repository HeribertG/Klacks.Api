// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Defines policy names and limits for ASP.NET Rate Limiting middleware.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class RateLimitingPolicies
{
    public const string Login = "login";
    public const string Upload = "upload";
    public const string RefreshToken = "refresh-token";

    public const int LoginPermitLimit = 20;
    public const int UploadPermitLimit = 30;
    public const int RefreshTokenPermitLimit = 30;
    public const int MaxBulkOperationItems = 500;

    public static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(1);
}
