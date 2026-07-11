// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the calculation macros (scripts) defined in the settings. Thin wrapper around
/// <see cref="Klacks.Api.Application.Queries.Settings.Macros.ListQuery"/>; an optional search
/// term filters by macro name.
/// </summary>
/// <param name="searchTerm">Optional. Filters the returned macros by name (case-insensitive);
/// when the substring filter finds nothing, the fuzzy MacroResolver suggests the closest macro.</param>

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

        var allMacros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();
        var macros = allMacros;
        var message = $"Found {macros.Count} macro(s).";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            macros = allMacros
                .Where(m => m.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
            message = $"Found {macros.Count} macro(s) matching '{searchTerm}'.";

            // A spoken or decorated name ("Makro-All-Shift") rarely survives a plain substring
            // filter — fall back to the fuzzy resolver instead of reporting an empty list.
            if (macros.Count == 0)
            {
                var (match, error) = MacroResolver.Resolve(allMacros, searchTerm);
                if (match != null)
                {
                    macros = [match];
                    message = $"No exact match for '{searchTerm}'; closest macro is '{match.Name}'. " +
                              "Tell the user you assumed this macro.";
                }
                else
                {
                    message = error!;
                }
            }
        }

        var resultData = new
        {
            Macros = macros.Select(m => new { m.Id, m.Name, m.Type, Category = m.Category.ToString() }).ToList(),
            Count = macros.Count
        };

        return SkillResult.SuccessResult(resultData, message);
    }
}
