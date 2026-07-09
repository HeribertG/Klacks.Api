// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the name arguments the model produced for a tool call to a concrete entity
/// and checks whether it is the entity the goldset expects. Used for the
/// resolved-entity-id slot match mode.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public interface ISlotEntityResolver
{
    Task<bool> ResolvesToExpectedEntityAsync(
        ExpectedEntityRef expected,
        IReadOnlyDictionary<string, object> toolParameters,
        CancellationToken cancellationToken = default);
}
