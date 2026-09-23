// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of delivering a clarification question over one channel. Never carries an exception: every
/// sender maps provider or SMTP failures to Failed with the error text.
/// </summary>
/// <param name="Success">True when the channel accepted the message</param>
/// <param name="Error">Error text of a failed delivery, null on success</param>

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record InboundReplyResult(bool Success, string? Error)
{
    public static InboundReplyResult Sent { get; } = new(true, null);

    public static InboundReplyResult Failed(string? error) => new(false, error);
}
