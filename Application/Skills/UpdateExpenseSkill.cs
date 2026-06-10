// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing expense entry: loads it via GetQuery&lt;ExpensesResource&gt;, patches only
/// the supplied fields (amount, description, taxable) and persists via PutCommand. Fields that are
/// not supplied keep their current value; a call without changes is a successful no-op.
/// </summary>
/// <param name="expenseId">UUID of the expense entry to update (required).</param>
/// <param name="amount">Optional new amount.</param>
/// <param name="description">Optional new description.</param>
/// <param name="taxable">Optional new taxable flag.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_expense")]
public class UpdateExpenseSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateExpenseSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var expenseId = GetRequiredGuid(parameters, "expenseId");

        ExpensesResource existing;
        try
        {
            existing = await _mediator.Send(new GetQuery<ExpensesResource>(expenseId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Expense {expenseId} not found.");
        }

        var changed = new List<string>();

        var amount = GetParameter<decimal?>(parameters, "amount");
        if (amount.HasValue && amount.Value != existing.Amount)
        {
            existing.Amount = amount.Value;
            changed.Add("amount");
        }

        var description = GetParameter<string>(parameters, "description");
        if (description != null && description.Trim() != existing.Description)
        {
            existing.Description = description.Trim();
            changed.Add("description");
        }

        var taxable = GetParameter<bool?>(parameters, "taxable");
        if (taxable.HasValue && taxable.Value != existing.Taxable)
        {
            existing.Taxable = taxable.Value;
            changed.Add("taxable");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ExpenseId = expenseId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — expense left unchanged.");
        }

        var resource = new ExpensesResource
        {
            Id = existing.Id,
            WorkId = existing.WorkId,
            Amount = existing.Amount,
            Description = existing.Description,
            Taxable = existing.Taxable
        };

        var updated = await _mediator.Send(new PutCommand<ExpensesResource>(resource), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Update of expense {expenseId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                ExpenseId = expenseId,
                ChangedFields = changed,
                updated.WorkId,
                updated.Amount,
                updated.Description,
                updated.Taxable
            },
            $"Expense {expenseId} updated ({string.Join(", ", changed)}).");
    }
}
