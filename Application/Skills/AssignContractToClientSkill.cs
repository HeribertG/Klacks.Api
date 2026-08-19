// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Staffs;

using Klacks.Api.Application.Mappers;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AssignContractToClientSkill : BaseSkillImplementation
{
    private const string SkillName = "assign_contract_to_client";

    private readonly IClientRepository _clientRepository;
    private readonly IContractRepository _contractRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public AssignContractToClientSkill(
        IClientRepository clientRepository,
        IContractRepository contractRepository,
        ClientMapper clientMapper,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes)
    {
        _clientRepository = clientRepository;
        _contractRepository = contractRepository;
        _clientMapper = clientMapper;
        _selfApi = selfApi;
        _routes = routes;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdStr = GetRequiredString(parameters, "clientId");
        var contractIdStr = GetRequiredString(parameters, "contractId");
        var fromDateStr = GetRequiredString(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var setAsActive = GetParameter<bool>(parameters, "setAsActive", true);

        if (!Guid.TryParse(clientIdStr, out var clientId))
        {
            return SkillResult.Error($"Invalid client ID format: {clientIdStr}");
        }

        if (!Guid.TryParse(contractIdStr, out var contractId))
        {
            return SkillResult.Error($"Invalid contract ID format: {contractIdStr}");
        }

        if (!DateOnly.TryParse(fromDateStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid from date format: {fromDateStr}. Expected format: YYYY-MM-DD");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrEmpty(untilDateStr))
        {
            if (!DateOnly.TryParse(untilDateStr, out var parsedUntil))
            {
                return SkillResult.Error($"Invalid until date format: {untilDateStr}. Expected format: YYYY-MM-DD");
            }
            untilDate = parsedUntil;
        }

        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return SkillResult.Error($"Client with ID {clientId} not found.");
        }

        var contract = await _contractRepository.Get(contractId);
        if (contract == null)
        {
            return SkillResult.Error($"Contract with ID {contractId} not found.");
        }

        var existingAssignment = client.ClientContracts
            .FirstOrDefault(cc => cc.ContractId == contractId && !cc.IsDeleted);

        if (existingAssignment != null)
        {
            return SkillResult.Error($"Contract '{contract.Name}' is already assigned to this client.");
        }

        if (setAsActive)
        {
            foreach (var cc in client.ClientContracts.Where(c => c.IsActive && !c.IsDeleted))
            {
                cc.IsActive = false;
                cc.UpdateTime = DateTime.UtcNow;
                cc.CurrentUserUpdated = context.UserName;
            }
        }

        var clientContract = new ClientContract
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            ContractId = contractId,
            FromDate = fromDate,
            UntilDate = untilDate,
            IsActive = setAsActive,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        client.ClientContracts.Add(clientContract);
        var result = await _selfApi.PutAsync<ClientResource>(
            _routes.Resolve(typeof(ClientResource)), _clientMapper.ToResource(client), context,
            SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        var resultData = new
        {
            ClientContractId = clientContract.Id,
            ClientId = clientId,
            ClientName = $"{client.FirstName} {client.Name}",
            ContractId = contractId,
            ContractName = contract.Name,
            FromDate = fromDate,
            UntilDate = untilDate,
            IsActive = setAsActive,
            GuaranteedHours = contract.GuaranteedHours,
            Percent = contract.Percent,
            MaximumHours = contract.MaximumHours
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Contract '{contract.Name}' successfully assigned to '{client.FirstName} {client.Name}'.");
    }
}
