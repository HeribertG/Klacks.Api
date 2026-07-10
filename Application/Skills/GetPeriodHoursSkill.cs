// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shows an employee's period-hours balance for a date range: actual scheduled hours,
/// surcharges, the contract's guaranteed (target) hours and the resulting balance — the
/// numbers the schedule shows in the employee row header. Scenario-aware via analyseToken.
/// </summary>
/// <param name="clientId">Required. UUID of the employee (client).</param>
/// <param name="startDate">Required. Period start (yyyy-MM-dd).</param>
/// <param name="endDate">Required. Period end (yyyy-MM-dd).</param>
/// <param name="analyseToken">Optional. Scenario token; omitted = production schedule.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.PeriodHours;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_period_hours")]
public class GetPeriodHoursSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetPeriodHoursSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");

        if (endDate < startDate)
        {
            return SkillResult.Error(
                $"endDate {endDate:yyyy-MM-dd} must not be before startDate {startDate:yyyy-MM-dd}.");
        }

        Guid? analyseToken = null;
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw))
        {
            if (!Guid.TryParse(analyseTokenRaw, out var parsedToken))
            {
                return SkillResult.Error($"Invalid analyseToken format: {analyseTokenRaw}");
            }

            analyseToken = parsedToken;
        }

        var request = new PeriodHoursRequest
        {
            ClientIds = new List<Guid> { clientId },
            StartDate = startDate,
            EndDate = endDate,
            AnalyseToken = analyseToken
        };

        var result = await _mediator.Send(new GetPeriodHoursQuery(request), cancellationToken);
        if (!result.TryGetValue(clientId, out var hours))
        {
            return SkillResult.Error(
                $"No period hours found for client {clientId} between {startDate:yyyy-MM-dd} and " +
                $"{endDate:yyyy-MM-dd} — check the client id and that the range matches a schedule period.");
        }

        var balance = hours.Hours - hours.GuaranteedHours;
        var balanceLabel = balance >= 0 ? $"+{balance}" : balance.ToString();

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                StartDate = startDate,
                EndDate = endDate,
                AnalyseToken = analyseToken,
                ActualHours = hours.Hours,
                hours.Surcharges,
                TargetHours = hours.GuaranteedHours,
                Balance = balance
            },
            $"Period {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}: {hours.Hours}h scheduled " +
            $"(+{hours.Surcharges}h surcharges) against {hours.GuaranteedHours}h guaranteed — " +
            $"balance {balanceLabel}h.");
    }
}
