// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reaches a recipient who holds no live connection, through a messenger channel. The core owns this
/// interface and never names a messaging implementation, so the trigger pipeline keeps working
/// unchanged when the messaging plugin is uninstalled or switched off: the implementation then
/// reports ChannelUnavailable and the caller simply has no loud channel that round.
/// </summary>
/// <param name="userId">AppUser the message is meant for</param>
/// <param name="message">Ready-to-send plain text; the implementation does not format anything</param>
/// <param name="triggerKind">Trigger kind, used for structured logging only</param>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IOfflineMessengerNotifier
{
    Task<OfflineMessengerDeliveryResult> TrySendAsync(
        string userId,
        string message,
        string triggerKind,
        CancellationToken cancellationToken = default);
}
