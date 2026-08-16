// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a proactive trigger event into the plain sentence that goes out over a messenger.
/// Separate from AgentTriggerService because resolving the installation language is a settings
/// lookup, and the dispatch loop must not grow a persistence dependency for it.
/// </summary>
/// <param name="triggerEvent">Event whose summary key and parameters are to be rendered</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveMessengerTextComposer
{
    Task<string> ComposeAsync(IAgentTriggerEvent triggerEvent, CancellationToken cancellationToken = default);
}
