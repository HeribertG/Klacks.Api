// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the full details of a single contract (hours, rates, validity, working days,
/// payment interval). Use list_contracts first to find the contract ID.
/// </summary>
/// <param name="contractId">Required. UUID of the contract to load.</param>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_contract_details")]
public class GetContractDetailsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetContractDetailsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var contractId = GetRequiredGuid(parameters, "contractId");

        ContractResource contract;
        try
        {
            contract = await _mediator.Send(new GetQuery<ContractResource>(contractId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Contract '{contractId}' not found.");
        }

        var resultData = new
        {
            contract.Id,
            contract.Name,
            contract.GuaranteedHours,
            contract.MinimumHours,
            contract.MaximumHours,
            contract.FullTime,
            contract.NightRate,
            contract.HolidayRate,
            contract.SaRate,
            contract.SoRate,
            contract.PaymentInterval,
            contract.ValidFrom,
            contract.ValidUntil,
            contract.CalendarSelectionId,
            contract.WorkOnMonday,
            contract.WorkOnTuesday,
            contract.WorkOnWednesday,
            contract.WorkOnThursday,
            contract.WorkOnFriday,
            contract.WorkOnSaturday,
            contract.WorkOnSunday,
            contract.PerformsShiftWork,
            contract.SchedulingRuleId
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Contract '{contract.Name}' loaded (id {contract.Id}).");
    }
}
