// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates which qualifications have enough clients to justify a new qualification group, so Klacksy
/// can recommend a concrete create_group call before the user has to guess. Thin wrapper that
/// dispatches <see cref="Klacks.Api.Application.Queries.Groups.EvaluateGroupingByQualificationQuery"/>.
/// The evaluation is advisory only — creating a group stays a manual step (create_group), and filling
/// it afterward stays a manual step too (fill_group_by_criteria).
/// </summary>
/// <param name="entityType">Which client population to evaluate: Employee, ExternEmp, or Customer.</param>

using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("evaluate_grouping_by_qualification")]
public class EvaluateGroupingByQualificationSkill : BaseSkillImplementation
{
    private const string EntityTypeParameterName = "entityType";

    private readonly IMediator _mediator;

    public EvaluateGroupingByQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var entityTypeValue = GetRequiredString(parameters, EntityTypeParameterName);
        if (!Enum.TryParse<EntityTypeEnum>(entityTypeValue, ignoreCase: true, out var entityType))
        {
            return SkillResult.Error(
                $"Invalid {EntityTypeParameterName} '{entityTypeValue}'. Must be one of: " +
                $"{EntityTypeEnum.Employee}, {EntityTypeEnum.ExternEmp}, {EntityTypeEnum.Customer}.");
        }

        var result = await _mediator.Send(
            new EvaluateGroupingByQualificationQuery(entityType), cancellationToken);

        return SkillResult.SuccessResult(result, result.Recommendation);
    }
}
