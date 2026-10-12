// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant.TurnIntent;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Supplies the trained turn-intent head, or null when the shipped weight file is missing or invalid.
/// </summary>
public interface ITurnIntentHeadProvider
{
    TurnIntentHeadWeights? Current { get; }
}
