// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs the correction/toolset pipeline both chat entry points need before calling ILLMService: peek the
/// previous-action anchor, plan a correction against it, assemble the toolset with the correction's
/// composite message and exclusions folded in, complete the correction against the assembled toolset,
/// pin its clarification candidates, and hold its undo offer as a one-time token. One implementation so
/// the streaming and non-streaming paths cannot diverge on any of it - the same reason
/// ITurnPreparationService exists for the rest of turn preparation.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface ICorrectionTurnPreparer
{
    Task<CorrectionTurnPreparation> PrepareAsync(
        Agent? agent,
        List<string> userRights,
        string message,
        string? conversationId,
        string userId,
        string? language,
        string? currentRoute,
        int maxToolsForProvider,
        CancellationToken cancellationToken);
}
