// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns the full details of a single contract (hours, time-credit rates, validity, working days,
/// shift-work flag, payment interval). A null rate or shift-work flag means "standard": the scheduling
/// rule decides, then the installation settings; 0 is an explicit "no credit". Use list_contracts first
/// to find the contract ID.
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
    private const string StandardValueNote =
        "Rates are time-credit factors (0.1 = 6 minutes per hour). A null rate or a null PerformsShiftWork means standard: the scheduling rule decides, then the installation settings. 0 means explicitly no credit. While shift work resolves to false, the contract's own rates are ignored.";

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
            contract.WE1Rate,
            contract.WE2Rate,
            contract.WE3Rate,
            contract.PaymentInterval,
            contract.Percent,
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
            contract.SchedulingRuleId,
            StandardValueNote
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Contract '{contract.Name}' loaded (id {contract.Id}).");
    }
}
