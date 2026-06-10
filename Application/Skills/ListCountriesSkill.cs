// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists all countries configured in the system via ListQuery&lt;CountryResource&gt;.
/// Returns id, abbreviation, phone prefix and the multilingual name per country.
/// Use this to find valid country abbreviations for address workflows and list_states.
/// </summary>

using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_countries")]
public class ListCountriesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListCountriesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var countries = await _mediator.Send(new ListQuery<CountryResource>(), cancellationToken);

        var projected = countries
            .Select(c => new
            {
                c.Id,
                c.Abbreviation,
                c.Prefix,
                Name = c.Name?.ToDictionary()
            })
            .OrderBy(c => c.Abbreviation)
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, Countries = projected },
            $"Found {projected.Count} countries.");
    }
}
