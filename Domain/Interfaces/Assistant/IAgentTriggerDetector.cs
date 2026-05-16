// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A trigger detector inspects domain state and emits zero or more IAgentTriggerEvent
/// instances to be dispatched by IAgentTriggerService. Implementations are stateless;
/// the scanning BackgroundService injects them all and calls DetectAsync each tick.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerDetector
{
    string Kind { get; }

    Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default);
}
