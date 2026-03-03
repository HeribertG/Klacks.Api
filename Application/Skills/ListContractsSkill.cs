// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListContractsSkill : BaseSkill
{
    private readonly IContractRepository _contractRepository;

    public override string Name => "list_contracts";

    public override string Description =>
        "Lists all available contracts in the system. Returns contract details including name, hours, and rates. " +
        "Use this to find contract IDs before assigning contracts to clients.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewClients" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "activeOnly",
            "If true, only return contracts that are currently valid (ValidFrom <= today and ValidUntil is null or >= today)",
            SkillParameterType.Boolean,
            Required: false,
            DefaultValue: "true")
    };

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
