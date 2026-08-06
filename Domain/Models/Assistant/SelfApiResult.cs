// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of a self-call against the own REST API. The failure text is already phrased for the model
/// to relay to the user — validation messages are unpacked from the ProblemDetails body, and a denied
/// request says so in plain words — so callers do not each re-interpret status codes.
/// </summary>
/// <param name="Success">True when the endpoint answered with a success status</param>
/// <param name="Value">The deserialized response body; null when the call failed or returned no content</param>
/// <param name="StatusCode">The HTTP status code, 0 when the request never reached the endpoint</param>
/// <param name="ErrorMessage">Ready-to-relay failure text; null on success</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SelfApiResult<T>(bool Success, T? Value, int StatusCode, string? ErrorMessage)
{
    public static SelfApiResult<T> Ok(T? value, int statusCode) => new(true, value, statusCode, null);

    public static SelfApiResult<T> Failed(int statusCode, string errorMessage) =>
        new(false, default, statusCode, errorMessage);
}
