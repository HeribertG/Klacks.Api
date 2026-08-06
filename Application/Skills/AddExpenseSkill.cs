// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds an expense entry to an existing Work entry by calling POST api/backend/Expenses on the own REST
/// API under the caller's token, so [Authorize], validation and the request log apply exactly as they do
/// to the browser. The Post handler enforces day-lock rules and triggers period-hour recalculation plus
/// schedule notifications. The workId must reference an existing Work (shift assignment).
/// </summary>
/// <param name="workId">UUID of the Work entry the expense belongs to (required).</param>
/// <param name="amount">Expense amount (required).</param>
/// <param name="description">Optional free-text description of the expense.</param>
/// <param name="taxable">Optional flag whether the expense is taxable; defaults to false.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AddExpenseSkill : BaseSkillImplementation
{
    private const string SkillName = "add_expense";

    private readonly IKlacksSelfApiClient _selfApi;

    public AddExpenseSkill(IKlacksSelfApiClient selfApi)
    {
        _selfApi = selfApi;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");
        var amount = GetParameter<decimal?>(parameters, "amount")
            ?? throw new ArgumentException("Required parameter 'amount' is missing");
        var description = GetParameter<string>(parameters, "description");
        var taxable = GetParameter<bool?>(parameters, "taxable") ?? false;

        var resource = new ExpensesResource
        {
            WorkId = workId,
            Amount = amount,
            Description = description?.Trim() ?? string.Empty,
            Taxable = taxable
        };

        var result = await _selfApi.PostAsync<ExpensesResource>(
            SelfApiRoutes.Expenses, resource, context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        var created = result.Value;
        if (created == null)
        {
            return SkillResult.Error($"Creating the expense for work {workId} returned no result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                created.Id,
                created.WorkId,
                created.Amount,
                created.Description,
                created.Taxable
            },
            $"Expense of {created.Amount} added to work {workId} (id {created.Id}).");
    }
}
