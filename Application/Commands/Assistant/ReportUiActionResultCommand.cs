// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The frontend reporting the real outcome of a UiAction it executed in the browser (W1.4). The
/// dispatch was booked as Dispatched; this command resolves the tracking id to the usage row and
/// moves it to Completed or Failed, which is the only honest signal the backend can have about
/// browser-side execution.
/// </summary>
/// <param name="UserId">Identity of the caller, taken from the token, never from the body</param>
/// <param name="TrackingId">The UiActionTrackingId the dispatch handed to the client</param>
/// <param name="Status">"completed" or "failed"</param>
/// <param name="ErrorMessage">Optional failure detail from the browser</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ReportUiActionResultCommand : IRequest<ReportUiActionResultResult>
{
    public string UserId { get; set; } = string.Empty;

    public Guid TrackingId { get; set; }

    public string Status { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}

public sealed record ReportUiActionResultResult(bool Found, bool Updated, string? Error);
