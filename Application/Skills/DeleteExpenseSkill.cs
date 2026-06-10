// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an expense entry via DeleteCommand&lt;ExpensesResource&gt;. The delete handler
/// enforces day-lock rules and triggers period-hour recalculation plus schedule notifications.
/// Inverse of add_expense for rollback purposes.
/// </summary>
/// <param name="expenseId">UUID of the expense entry to delete (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_expense")]
public class DeleteExpenseSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteExpenseSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var expenseId = GetRequiredGuid(parameters, "expenseId");

        var deleted = await _mediator.Send(new DeleteCommand<ExpensesResource>(expenseId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Expense {expenseId} not found.");
        }

        return SkillResult.SuccessResult(
            new
            {
                ExpenseId = expenseId,
                deleted.WorkId,
                deleted.Amount,
                deleted.Description
            },
            $"Expense {expenseId} (work {deleted.WorkId}, amount {deleted.Amount}) deleted.");
    }
}
