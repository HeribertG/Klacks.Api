// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal single-purpose skill: assigns a contract to a client, both identified by name.
/// Replaces assign_contract_to_client for chat-driven flows where GUIDs are not available.
/// </summary>
/// <param name="firstName">First name of the client.</param>
/// <param name="lastName">Last name of the client.</param>
/// <param name="contractName">Name (or partial name) of the contract to assign.</param>
/// <param name="fromDate">Contract start date (YYYY-MM-DD).</param>
/// <param name="untilDate">Optional contract end date (YYYY-MM-DD); leave empty for indefinite.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("assign_contract_by_name")]
public class AssignContractByNameSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignContractByNameSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IContractRepository contractRepository,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _contractRepository = contractRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var contractName = GetRequiredString(parameters, "contractName");
        var fromDateStr = GetRequiredString(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");

        if (!DateOnly.TryParse(fromDateStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate format: '{fromDateStr}'. Expected YYYY-MM-DD.");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrEmpty(untilDateStr))
        {
            if (!DateOnly.TryParse(untilDateStr, out var parsedUntil))
            {
                return SkillResult.Error($"Invalid untilDate format: '{untilDateStr}'. Expected YYYY-MM-DD.");
            }
            untilDate = parsedUntil;
        }

        var (client, clientError) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (clientError != null)
        {
            return SkillResult.Error(clientError);
        }

        var allContracts = await _contractRepository.List();
        var matches = allContracts
            .Where(c => !c.IsDeleted && c.Name.Contains(contractName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            var availableContracts = allContracts.Where(c => !c.IsDeleted).Select(c => c.Name).ToList();
            var available = availableContracts.Count > 0
                ? "Available contracts: " + string.Join(", ", availableContracts) + "."
                : "There are no contracts yet.";
            return SkillResult.Error(
                $"No contract found matching '{contractName}'. {available} " +
                "Offer the user only these real contract names — do not invent contracts.");
        }

        if (matches.Count > 1)
        {
            var names = string.Join(", ", matches.Select(c => $"'{c.Name}'"));
            return SkillResult.Error($"Multiple contracts match '{contractName}': {names}. Please be more specific.");
        }

        var contract = matches[0];

        var existing = client!.ClientContracts
            .FirstOrDefault(cc => cc.ContractId == contract.Id && !cc.IsDeleted);
        if (existing != null)
        {
            return SkillResult.Error($"Contract '{contract.Name}' is already assigned to {client.FirstName} {client.Name}.");
        }

        var now = DateTime.UtcNow;
        foreach (var cc in client.ClientContracts.Where(c => c.IsActive && !c.IsDeleted))
        {
            cc.IsActive = false;
            cc.UpdateTime = now;
            cc.CurrentUserUpdated = context.UserName;
        }

        client.ClientContracts.Add(new ClientContract
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            ContractId = contract.Id,
            FromDate = fromDate,
            UntilDate = untilDate,
            IsActive = true,
            CreateTime = now,
            CurrentUserCreated = context.UserName
        });

        client.UpdateTime = now;
        client.CurrentUserUpdated = context.UserName;

        await _clientRepository.Put(client);
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                ClientId = client.Id,
                ClientName = $"{client.FirstName} {client.Name}",
                ContractId = contract.Id,
                ContractName = contract.Name,
                FromDate = fromDate,
                UntilDate = untilDate
            },
            $"Contract '{contract.Name}' assigned to {client.FirstName} {client.Name} from {fromDate}.");
    }
}
