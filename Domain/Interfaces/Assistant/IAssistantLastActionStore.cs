// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Synchronous facade over the previous-action rows, used by the chat loop and by the two chat entry
/// points. Save replaces the record of a conversation (called only on turns that made a tool call);
/// MarkSuperseded leaves the record in place but stops it from anchoring a correction;
/// SaveClarificationCandidates records the two options a clarification question offered, so the next
/// turn's toolset can pin them; Peek reads the record back.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAssistantLastActionStore
{
    void Save(AssistantLastAction action);

    AssistantLastAction? Peek(Guid userId, string conversationId);

    void MarkSuperseded(Guid userId, string conversationId);

    void SaveClarificationCandidates(Guid userId, string conversationId, IReadOnlyList<string> skillNames);
}
