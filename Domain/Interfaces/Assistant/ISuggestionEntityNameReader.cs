// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only access to the real entity names a recipe ask-step slot refers to (e.g. contract or
/// group names), used to ground LLM-generated suggestion chips against the database.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISuggestionEntityNameReader
{
    /// <summary>
    /// Returns the real, non-deleted entity names for the given recipe slot, or null when the slot
    /// is not one this reader knows how to ground (in which case the caller must not filter).
    /// </summary>
    /// <param name="slotName">The recipe step's slot name, e.g. "contractName" or "groupName".</param>
    Task<IReadOnlyList<string>?> GetRealNamesForSlotAsync(string slotName, CancellationToken cancellationToken = default);
}
