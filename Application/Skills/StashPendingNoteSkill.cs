// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("stash_pending_note")]
public class StashPendingNoteSkill : BaseSkillImplementation
{
    private readonly IPendingUserNoteRepository _noteRepository;
    private readonly IAgentRepository _agentRepository;

    public StashPendingNoteSkill(
        IPendingUserNoteRepository noteRepository,
        IAgentRepository agentRepository)
    {
        _noteRepository = noteRepository;
        _agentRepository = agentRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var content = GetRequiredString(parameters, "content");
        var topic = GetParameter<string>(parameters, "topic");
        var forEveryone = GetParameter<bool?>(parameters, "forEveryone") ?? false;
        Guid? userId = forEveryone ? null : GetParameter<Guid?>(parameters, "userId") ?? context.UserId;

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No agent is configured yet.");
        }

        var note = new PendingUserNote
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            UserId = userId,
            Content = content,
            Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim()
        };

        await _noteRepository.AddAsync(note, cancellationToken);

        var audience = userId == null ? "every user (broadcast)" : "the user";
        return SkillResult.SuccessResult(
            new { NoteId = note.Id, note.Topic, ForUserId = userId, Broadcast = userId == null },
            $"Stashed a pending note{(note.Topic != null ? $" [{note.Topic}]" : "")} to relay to {audience} later.");
    }
}
