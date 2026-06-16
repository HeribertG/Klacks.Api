// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing client communication entry (email/phone/note). Only fields supplied as
/// parameters are changed; the entry is loaded via GetQuery&lt;CommunicationResource&gt; and saved
/// via PutCommand&lt;CommunicationResource&gt;. Use get_client_details to resolve the id first.
/// </summary>
/// <param name="communicationId">Required. UUID of the communication entry to update.</param>
/// <param name="value">Optional. New value (the email address, phone number or note text).</param>
/// <param name="type">Optional. New numeric communication type code (e.g. 4 = private mail, 1 = private cell phone).</param>
/// <param name="description">Optional. New free-text description/label for the entry.</param>
/// <param name="prefix">Optional. New dialling prefix (country/area code) for phone entries.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_communication")]
public class UpdateCommunicationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateCommunicationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var communicationId = GetRequiredGuid(parameters, "communicationId");

        CommunicationResource communication;
        try
        {
            communication = await _mediator.Send(new GetQuery<CommunicationResource>(communicationId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Communication '{communicationId}' not found.");
        }

        var changed = new List<string>();

        var value = GetParameter<string>(parameters, "value");
        if (!string.IsNullOrWhiteSpace(value) && value.Trim() != communication.Value)
        {
            communication.Value = value.Trim();
            changed.Add("value");
        }

        var type = GetParameter<int?>(parameters, "type");
        if (type.HasValue && (int)communication.Type != type.Value)
        {
            communication.Type = (CommunicationTypeEnum)type.Value;
            changed.Add("type");
        }

        var description = GetParameter<string>(parameters, "description");
        if (description != null && description != communication.Description)
        {
            communication.Description = description;
            changed.Add("description");
        }

        var prefix = GetParameter<string>(parameters, "prefix");
        if (prefix != null && prefix != communication.Prefix)
        {
            communication.Prefix = prefix;
            changed.Add("prefix");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { CommunicationId = communicationId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — communication left unchanged.");
        }

        var updated = await _mediator.Send(new PutCommand<CommunicationResource>(communication), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating communication '{communicationId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                CommunicationId = communicationId,
                ChangedFields = changed,
                updated.ClientId,
                updated.Type,
                updated.Value
            },
            $"Communication entry updated ({string.Join(", ", changed)}).");
    }
}
