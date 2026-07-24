// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Set of per-turn budget caps derived from a model's effective input token limit by
/// <see cref="Klacks.Api.Domain.Interfaces.Assistant.IContextBudgetPolicy"/>. Threaded through the
/// chat pipeline instead of the previous fixed constants so a small-context model cannot be handed a
/// prompt shaped for a 200k-token model.
/// </summary>
/// <param name="MaxHistoryMessages">Ceiling on the number of conversation-history messages kept after truncation.</param>
/// <param name="MaxToolsForProvider">Ceiling on the number of tool/function definitions sent to the provider.</param>
/// <param name="MaxPinnedMemories">Ceiling on pinned agent memories rendered into the prompt.</param>
/// <param name="MaxMemoriesPerTurn">Ceiling on hybrid-searched (non-pinned) memories rendered per turn.</param>
/// <param name="MaxToolResultChars">Ceiling on characters of a single tool-call result fed back into the loop.</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ContextBudgetProfile(
    int MaxHistoryMessages,
    int MaxToolsForProvider,
    int MaxPinnedMemories,
    int MaxMemoriesPerTurn,
    int MaxToolResultChars);
