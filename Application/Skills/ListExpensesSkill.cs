// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the expense entries of one scope via ListExpensesInScopeQuery: the main plan by default, or the scenario
/// named by analyseToken - never both mixed. Each expense belongs to a Work entry (workId) and carries amount,
/// description and the taxable flag (false = Spese, true = Vergütung). Use this to find expense IDs before
/// update_expense / delete_expense (pass the same analyseToken to update_expense).
/// </summary>
/// <param name="analyseToken">Optional scenario UUID; omitted = main plan.</param>

using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_expenses")]
public class ListExpensesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListExpensesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var expenses = await _mediator.Send(new ListExpensesInScopeQuery(analyseToken), cancellationToken);

        var projected = expenses
            .Select(e => new { e.Id, e.WorkId, e.Amount, e.Description, e.Taxable })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, AnalyseToken = analyseToken, Expenses = projected },
            $"Found {projected.Count} expense entries.");
    }
}
