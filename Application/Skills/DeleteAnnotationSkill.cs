// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client annotation (note) via DeleteCommand&lt;AnnotationResource&gt;. Use
/// get_client_details to resolve the annotation id first; fails with a clear message if it does not exist.
/// </summary>
/// <param name="annotationId">Required. UUID of the annotation to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_annotation")]
public class DeleteAnnotationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteAnnotationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var annotationId = GetRequiredGuid(parameters, "annotationId");

        var deleted = await _mediator.Send(new DeleteCommand<AnnotationResource>(annotationId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Annotation {annotationId} not found.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id },
            "Annotation deleted.");
    }
}
