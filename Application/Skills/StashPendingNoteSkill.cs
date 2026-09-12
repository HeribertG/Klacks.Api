// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stashes a note to be relayed later. A note addressed at somebody else — a named recipient or the
/// whole installation — is an administrator action: every authenticated caller holds the Planer floor,
/// so without this gate any of them could place text in front of every user of the installation.
/// A caller without that right may still stash a note for themselves.
/// </summary>
/// <param name="noteRepository">Persists the stashed note</param>
/// <param name="agentRepository">Resolves the default agent a stashed note has to belong to</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("stash_pending_note")]
public class StashPendingNoteSkill : BaseSkillImplementation
{
    private const string ForeignRecipientRefusal =
        "Only an administrator can leave a note for somebody else or for everyone. " +
        "I can save this note for you alone instead — say so and I will.";

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
        var requestedUserId = GetParameter<Guid?>(parameters, "userId");

        var addressesSomebodyElse = forEveryone
            || (requestedUserId.HasValue && requestedUserId.Value != context.UserId);

        if (addressesSomebodyElse && !Permissions.HasPermission(context.UserPermissions, Roles.Admin))
        {
            return SkillResult.Error(ForeignRecipientRefusal);
        }

        Guid? userId = forEveryone ? null : requestedUserId ?? context.UserId;

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
