// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of one attempt to reach an offline recipient through a messenger channel. Carries the
/// provider's verdict rather than a bare bool so a refused send can be logged as such: Klacksy does
/// not know whether a message was read, but it does know when sending failed, and a report that
/// stays silent about that claims an alert which never left the building.
/// </summary>
/// <param name="Outcome">What happened to the attempt</param>
/// <param name="Channel">Messenger channel that was addressed; null when nothing was attempted</param>
/// <param name="ErrorMessage">Provider error text when the outcome is Failed or Throttled</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record OfflineMessengerDeliveryResult(
    OfflineMessengerDeliveryOutcome Outcome,
    string? Channel = null,
    string? ErrorMessage = null)
{
    public static OfflineMessengerDeliveryResult ChannelUnavailable { get; } =
        new(OfflineMessengerDeliveryOutcome.ChannelUnavailable);

    public static OfflineMessengerDeliveryResult NoContact { get; } =
        new(OfflineMessengerDeliveryOutcome.NoContact);

    public static OfflineMessengerDeliveryResult Sent(string channel) =>
        new(OfflineMessengerDeliveryOutcome.Sent, channel);

    public static OfflineMessengerDeliveryResult Failed(string channel, string? errorMessage) =>
        new(OfflineMessengerDeliveryOutcome.Failed, channel, errorMessage);

    public static OfflineMessengerDeliveryResult Throttled(string channel, string? errorMessage) =>
        new(OfflineMessengerDeliveryOutcome.Throttled, channel, errorMessage);
}
