// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Plugin.Contracts;

namespace Klacks.Api.Application.Interfaces.Plugins;

/// <summary>
/// Runs the channel-neutral inbound kernel (analyze, persist, execute, notify) for one messenger message
/// that was resolved to a known client. Scoped: the background consumer resolves it from a fresh scope
/// per message.
/// </summary>
public interface IMessengerIntentProcessor
{
    Task ProcessAsync(InboundClientMessengerMessage message, CancellationToken cancellationToken);
}
