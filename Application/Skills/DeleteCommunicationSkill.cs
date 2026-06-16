// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client communication entry (email/phone/note) via
/// DeleteCommand&lt;CommunicationResource&gt;. Use get_client_details first to resolve the id.
/// </summary>
/// <param name="communicationId">Required. UUID of the communication entry to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_communication")]
public class DeleteCommunicationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteCommunicationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var communicationId = GetRequiredGuid(parameters, "communicationId");

        var deleted = await _mediator.Send(new DeleteCommand<CommunicationResource>(communicationId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Communication {communicationId} not found.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id },
            "Communication entry deleted.");
    }
}
