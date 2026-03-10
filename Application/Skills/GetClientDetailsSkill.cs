// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_client_details")]
public class GetClientDetailsSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;

    public GetClientDetailsSkill(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdStr = GetRequiredString(parameters, "clientId");

        if (!Guid.TryParse(clientIdStr, out var clientId))
        {
            return SkillResult.Error($"Invalid client ID format: {clientIdStr}");
        }

        var client = await _clientRepository.Get(clientId);

        if (client == null)
        {
            return SkillResult.Error($"Client with ID {clientId} not found.");
        }

        var contracts = client.ClientContracts
            .Where(cc => !cc.IsDeleted)
            .Select(cc => new
            {
                cc.Id,
                ContractId = cc.ContractId,
                ContractName = cc.Contract?.Name,
                cc.FromDate,
                cc.UntilDate,
                cc.IsActive,
                GuaranteedHours = cc.Contract?.GuaranteedHours,
                MaximumHours = cc.Contract?.MaximumHours
            })
            .ToList();

        var groups = client.GroupItems
            .Where(gi => !gi.IsDeleted && gi.Group != null)
            .Select(gi => new
            {
                gi.Id,
                GroupId = gi.GroupId,
                GroupName = gi.Group?.Name,
                gi.ValidFrom,
                gi.ValidUntil
            })
            .ToList();

        var addresses = client.Addresses
            .Where(a => !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.Street,
                a.Zip,
                a.City,
                Canton = a.State,
                a.Country,
                a.Type,
                a.ValidFrom
            })
            .ToList();

        var communications = client.Communications
            .Where(c => !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.Type,
                c.Value
            })
            .ToList();

        var activeContract = contracts.FirstOrDefault(c => c.IsActive);

        var resultData = new
        {
            Client = new
            {
                client.Id,
                client.IdNumber,
                client.FirstName,
                LastName = client.Name,
                client.Gender,
                client.Birthdate,
                client.Company,
                client.LegalEntity,
                client.Type
            },
            Contracts = contracts,
            ActiveContract = activeContract,
            Groups = groups,
            Addresses = addresses,
            Communications = communications,
            Summary = new
            {
                TotalContracts = contracts.Count,
                HasActiveContract = activeContract != null,
                TotalGroups = groups.Count,
                TotalAddresses = addresses.Count
            }
        };

        var message = $"Client '{client.FirstName} {client.Name}' has {contracts.Count} contract(s) " +
                      $"and belongs to {groups.Count} group(s).";

        return SkillResult.SuccessResult(resultData, message);
    }
}
