// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Outcome of one attempt to reach a recipient who holds no live connection through a messenger.
/// Distinguishes "nothing was tried" from "the provider refused", because only the latter means a
/// message the recipient was promised never went out.
/// </summary>
public enum OfflineMessengerDeliveryOutcome
{
    /// <summary>No messenger channel exists at all because the messaging plugin is absent or switched off.</summary>
    ChannelUnavailable = 0,

    /// <summary>A channel exists, but the recipient has no messenger identity registered.</summary>
    NoContact = 1,

    /// <summary>The provider accepted the message.</summary>
    Sent = 2,

    /// <summary>The provider refused the send (bot blocked, channel dead, configuration broken).</summary>
    Failed = 3,

    /// <summary>The provider refused because of rate limiting; the recipient itself is fine.</summary>
    Throttled = 4
}
