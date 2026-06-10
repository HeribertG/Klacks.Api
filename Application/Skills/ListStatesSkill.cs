// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists all states/cantons via ListQuery&lt;StateResource&gt;, optionally filtered by country.
/// Returns id, abbreviation (e.g. 'BE', 'ZH'), country prefix and the multilingual name.
/// </summary>
/// <param name="country">Optional country abbreviation (e.g. 'CH', 'DE', 'AT') to filter states by their CountryPrefix</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_states")]
public class ListStatesSkill : BaseSkillImplementation
{
    private const string CountryParameterName = "country";

    private readonly IMediator _mediator;

    public ListStatesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var country = GetParameter<string>(parameters, CountryParameterName)?.Trim();

        var states = await _mediator.Send(new ListQuery<StateResource>(), cancellationToken);

        var filtered = string.IsNullOrWhiteSpace(country)
            ? states
            : states.Where(s => string.Equals(s.CountryPrefix, country, StringComparison.OrdinalIgnoreCase));

        var projected = filtered
            .Select(s => new
            {
                s.Id,
                s.Abbreviation,
                s.CountryPrefix,
                Name = s.Name?.ToDictionary()
            })
            .OrderBy(s => s.CountryPrefix)
            .ThenBy(s => s.Abbreviation)
            .ToList();

        if (!string.IsNullOrWhiteSpace(country) && projected.Count == 0)
        {
            return SkillResult.Error($"No states found for country '{country}'. Use list_countries to see valid country abbreviations.");
        }

        var scope = string.IsNullOrWhiteSpace(country) ? string.Empty : $" for country '{country.ToUpperInvariant()}'";
        return SkillResult.SuccessResult(
            new { Count = projected.Count, States = projected },
            $"Found {projected.Count} states/cantons{scope}.");
    }
}
