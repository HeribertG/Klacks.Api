// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists all expense entries via ListQuery&lt;ExpensesResource&gt;. Each expense belongs to a Work
/// entry (workId) and carries amount, description and the taxable flag. Use this to find expense
/// IDs before update_expense / delete_expense.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
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
        var expenses = await _mediator.Send(new ListQuery<ExpensesResource>(), cancellationToken);

        var projected = expenses
            .Select(e => new { e.Id, e.WorkId, e.Amount, e.Description, e.Taxable })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, Expenses = projected },
            $"Found {projected.Count} expense entries.");
    }
}
