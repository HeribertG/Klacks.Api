// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a qualification master entry. Verifies the qualification exists by resolving it
/// by id or unambiguous name via the qualification list query, then dispatches a
/// <see cref="Klacks.Api.Application.Commands.Qualifications.DeleteQualificationCommand"/>.
/// </summary>
/// <param name="qualificationId">Id of the qualification to delete. Preferred over qualificationName.</param>
/// <param name="qualificationName">Exact qualification name used when qualificationId is omitted.</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_qualification")]
public class DeleteQualificationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (existing, resolveError) = await QualificationResolver.ResolveAsync(
            _mediator,
            GetParameter<string>(parameters, "qualificationId"),
            GetParameter<string>(parameters, "qualificationName"),
            cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error(resolveError!);
        }

        await _mediator.Send(new DeleteQualificationCommand(existing.Id), cancellationToken);

        return SkillResult.SuccessResult(
            new { existing.Id, existing.Name },
            $"Qualification '{QualificationResolver.DisplayName(existing)}' deleted.");
    }
}
