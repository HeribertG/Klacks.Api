// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decorator that keeps the previous-action record out of the database on the Playground: the record holds the visitor's
/// last message (up to 2000 characters), the arguments and results of the tool calls and the assistant's answer, and its
/// key is the shared demo admin plus the client-supplied conversation id. On the Playground nothing is saved and every
/// read finds no record, so a correction turn simply runs as a normal turn without an anchor. With the Playground mode
/// off each call is forwarded unchanged.
/// </summary>
/// <param name="inner">The real, database-backed store</param>
/// <param name="mode">Tells whether the instance is the Playground</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Playground;

public sealed class PlaygroundSilentAssistantLastActionStore : IAssistantLastActionStore
{
    private readonly IAssistantLastActionStore _inner;
    private readonly IPlaygroundMode _mode;

    public PlaygroundSilentAssistantLastActionStore(IAssistantLastActionStore inner, IPlaygroundMode mode)
    {
        _inner = inner;
        _mode = mode;
    }

    public void Save(AssistantLastAction action)
    {
        if (!_mode.IsActive)
        {
            _inner.Save(action);
        }
    }

    public AssistantLastAction? Peek(Guid userId, string conversationId) =>
        _mode.IsActive ? null : _inner.Peek(userId, conversationId);

    public void MarkSuperseded(Guid userId, string conversationId)
    {
        if (!_mode.IsActive)
        {
            _inner.MarkSuperseded(userId, conversationId);
        }
    }

    public void SaveClarificationCandidates(Guid userId, string conversationId, IReadOnlyList<string> skillNames)
    {
        if (!_mode.IsActive)
        {
            _inner.SaveClarificationCandidates(userId, conversationId, skillNames);
        }
    }
}
