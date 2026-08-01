// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates the default ERP drop point: reads it via GetDefaultQuery, patches only the supplied
/// fields and persists via PutCommand. Fields that are not supplied keep their current value.
/// </summary>
/// <param name="name">Optional new name of the drop point.</param>
/// <param name="sourceSystemId">Optional new identifier of the delivering system.</param>
/// <param name="bucketPrefix">Optional new path prefix files are picked up from.</param>
/// <param name="isEnabled">Optional new switch: whether files are picked up at all.</param>

using Klacks.Api.Application.Commands.ErpDropPoints;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_erp_drop_point_settings")]
public class UpdateErpDropPointSettingsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateErpDropPointSettingsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var existing = await _mediator.Send(new GetDefaultQuery(), cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error("No default drop point is configured.");
        }

        var changed = new List<string>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != existing.Name)
        {
            existing.Name = name.Trim();
            changed.Add("name");
        }

        var sourceSystemId = GetParameter<string>(parameters, "sourceSystemId");
        if (!string.IsNullOrWhiteSpace(sourceSystemId) && sourceSystemId.Trim() != existing.SourceSystemId)
        {
            existing.SourceSystemId = sourceSystemId.Trim();
            changed.Add("sourceSystemId");
        }

        var bucketPrefix = GetParameter<string>(parameters, "bucketPrefix");
        if (bucketPrefix != null && bucketPrefix.Trim() != existing.BucketPrefix)
        {
            existing.BucketPrefix = bucketPrefix.Trim();
            changed.Add("bucketPrefix");
        }

        var isEnabled = GetParameter<bool?>(parameters, "isEnabled");
        if (isEnabled.HasValue && isEnabled.Value != existing.IsEnabled)
        {
            existing.IsEnabled = isEnabled.Value;
            changed.Add("isEnabled");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { existing.Id, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — the drop point was left unchanged.");
        }

        ErpDropPointResource? updated;
        try
        {
            updated = await _mediator.Send(new PutCommand(existing), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        if (updated == null)
        {
            return SkillResult.Error("Updating the drop point returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                updated.Id,
                ChangedFields = changed,
                updated.Name,
                updated.SourceSystemId,
                updated.BucketPrefix,
                updated.IsEnabled
            },
            $"Drop point '{updated.Name}' updated ({string.Join(", ", changed)}).");
    }
}
