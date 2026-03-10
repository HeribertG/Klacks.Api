// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_contracts")]
public class ListContractsSkill : BaseSkillImplementation
{
    private readonly IContractRepository _contractRepository;

    public ListContractsSkill(IContractRepository contractRepository)
    {
        _contractRepository = contractRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var activeOnly = GetParameter<bool>(parameters, "activeOnly", true);

        var allContracts = await _contractRepository.List();
        var today = DateTime.UtcNow.Date;

        var contracts = allContracts
            .Where(c => !c.IsDeleted)
            .Where(c => !activeOnly || (c.ValidFrom <= today && (c.ValidUntil == null || c.ValidUntil >= today)))
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.GuaranteedHours,
                c.MinimumHours,
                c.MaximumHours,
                c.FullTime,
                c.PaymentInterval,
                c.ValidFrom,
                c.ValidUntil,
                c.NightRate,
                c.HolidayRate,
                c.SaRate,
                c.SoRate
            })
            .ToList();

        var resultData = new
        {
            Contracts = contracts,
            TotalCount = contracts.Count,
            ActiveOnly = activeOnly
        };

        var message = $"Found {contracts.Count} contract(s)" + (activeOnly ? " (active only)" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
