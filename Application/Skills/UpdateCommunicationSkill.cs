// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing client communication entry (email/phone/note). Only fields supplied as
/// parameters are changed; the entry is loaded via GetQuery&lt;CommunicationResource&gt; and saved
/// via PutCommand&lt;CommunicationResource&gt;. Use get_client_details to resolve the id first.
/// The write is self-verifying: the entry is re-read after the update and every changed field
/// must hold its new value before success is reported.
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
    private const string ValueField = "value";
    private const string TypeField = "type";
    private const string DescriptionField = "description";
    private const string PrefixField = "prefix";

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

        var value = GetParameter<string>(parameters, ValueField);
        if (!string.IsNullOrWhiteSpace(value) && value.Trim() != communication.Value)
        {
            communication.Value = value.Trim();
            changed.Add(ValueField);
        }

        var type = GetParameter<int?>(parameters, TypeField);
        if (type.HasValue && (int)communication.Type != type.Value)
        {
            communication.Type = (CommunicationTypeEnum)type.Value;
            changed.Add(TypeField);
        }

        var description = GetParameter<string>(parameters, DescriptionField);
        if (description != null && description != communication.Description)
        {
            communication.Description = description;
            changed.Add(DescriptionField);
        }

        var prefix = GetParameter<string>(parameters, PrefixField);
        if (prefix != null && prefix != communication.Prefix)
        {
            communication.Prefix = prefix;
            changed.Add(PrefixField);
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

        CommunicationResource persisted;
        try
        {
            persisted = await _mediator.Send(new GetQuery<CommunicationResource>(communicationId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error(
                $"Database verification failed: communication '{communicationId}' could not be re-read after the update.");
        }

        var mismatched = new List<string>();
        if (changed.Contains(ValueField) && persisted.Value != communication.Value)
        {
            mismatched.Add(ValueField);
        }

        if (changed.Contains(TypeField) && persisted.Type != communication.Type)
        {
            mismatched.Add(TypeField);
        }

        if (changed.Contains(DescriptionField) && persisted.Description != communication.Description)
        {
            mismatched.Add(DescriptionField);
        }

        if (changed.Contains(PrefixField) && persisted.Prefix != communication.Prefix)
        {
            mismatched.Add(PrefixField);
        }

        if (mismatched.Count > 0)
        {
            return SkillResult.Error(
                $"Database verification failed: field(s) {string.Join(", ", mismatched)} of communication " +
                $"'{communicationId}' do not hold the new value(s) after re-reading — the update did not persist as requested.");
        }

        return SkillResult.SuccessResult(
            new
            {
                CommunicationId = communicationId,
                ChangedFields = changed,
                persisted.ClientId,
                persisted.Type,
                persisted.Value
            },
            $"Communication entry updated ({string.Join(", ", changed)}) and confirmed in the database (verified).");
    }
}
