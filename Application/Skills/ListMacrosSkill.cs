// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the calculation macros (scripts) defined in the settings. Thin wrapper around
/// <see cref="Klacks.Api.Application.Queries.Settings.Macros.ListQuery"/>; an optional search
/// term filters by macro name.
/// </summary>
/// <param name="searchTerm">Optional. Filters the returned macros by name (case-insensitive).</param>

using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_macros")]
public class ListMacrosSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListMacrosSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            macros = macros
                .Where(m => m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var resultData = new
        {
            Macros = macros.Select(m => new { m.Id, m.Name, m.Type }).ToList(),
            Count = macros.Count
        };

        var message = $"Found {macros.Count} macro(s)" +
                      (!string.IsNullOrWhiteSpace(searchTerm) ? $" matching '{searchTerm}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
