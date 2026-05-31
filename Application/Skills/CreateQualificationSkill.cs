// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a qualification master entry (e.g. "Forklift licence", "First aid") that shifts can then
/// require and employees can hold. Thin wrapper around
/// <see cref="Klacks.Api.Application.Commands.Qualifications.CreateQualificationCommand"/>; calling it
/// with an existing name returns that qualification instead of creating a duplicate.
/// </summary>
/// <param name="name">Required. The qualification name.</param>
/// <param name="description">Optional. A short description.</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_qualification")]
public class CreateQualificationSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateQualificationSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("name is required.");
        }
        var description = GetParameter<string>(parameters, "description");

        var id = await _mediator.Send(new CreateQualificationCommand(name, description), cancellationToken);

        return SkillResult.SuccessResult(
            new { Id = id, Name = name.Trim() },
            $"Qualification '{name.Trim()}' is available (id {id}).");
    }
}
