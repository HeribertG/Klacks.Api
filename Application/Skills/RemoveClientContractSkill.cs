// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detaches a client's assignment to one of their contracts, identified by name or left out when the
/// client has only one. Verifies the removal against the database after saving.
/// </summary>
/// <param name="firstName">Optional. First name of the client.</param>
/// <param name="lastName">Required. Last name of the client.</param>
/// <param name="contractName">Optional. Name of the contract to detach; required only when more than one is assigned.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

using Klacks.Api.Application.DTOs.Staffs;

using Klacks.Api.Application.Mappers;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class RemoveClientContractSkill : BaseSkillImplementation
{
    private const string SkillName = "remove_client_contract";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ILogger<RemoveClientContractSkill> _logger;

    public RemoveClientContractSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        ClientMapper clientMapper,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ILogger<RemoveClientContractSkill> logger)
    {
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _clientMapper = clientMapper;
        _selfApi = selfApi;
        _routes = routes;
        _logger = logger;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetRequiredString(parameters, "lastName");
        var contractName = GetParameter<string>(parameters, "contractName");

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var assignments = client!.ClientContracts.ToList();
        if (assignments.Count == 0)
        {
            return SkillResult.Error($"{client.FirstName} {client.Name} has no contract assigned.");
        }

        var candidates = string.IsNullOrWhiteSpace(contractName)
            ? assignments
            : assignments
                .Where(cc => cc.Contract != null &&
                             cc.Contract.Name.Contains(contractName!, StringComparison.OrdinalIgnoreCase))
                .ToList();

        if (candidates.Count == 0)
        {
            var assigned = string.Join(", ", assignments.Select(cc => cc.Contract?.Name ?? "(unnamed)"));
            return SkillResult.Error(
                $"No contract matching '{contractName}' is assigned to {client.FirstName} {client.Name}. " +
                $"Currently assigned: {assigned}.");
        }

        if (candidates.Count > 1)
        {
            var options = candidates
                .Select(cc => new
                {
                    cc.Id,
                    Name = cc.Contract?.Name,
                    cc.FromDate,
                    cc.UntilDate,
                    cc.IsActive
                })
                .ToList();

            return SkillResult.SuccessResult(
                new { ClientId = client.Id, Candidates = options },
                $"{client.FirstName} {client.Name} has {candidates.Count} matching contracts. Specify which one by name or period.");
        }

        var target = candidates[0];
        var contractLabel = target.Contract?.Name ?? "(unnamed)";
        var targetId = target.Id;

        client.ClientContracts.Remove(target);
        client.UpdateTime = DateTime.UtcNow;
        client.CurrentUserUpdated = context.UserName;

        var result = await _selfApi.PutAsync<ClientResource>(
            _routes.Resolve(typeof(ClientResource)), _clientMapper.ToResource(client), context,
            SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new { ClientId = client.Id, client.FirstName, LastName = client.Name, ContractName = contractLabel },
            $"Contract '{contractLabel}' detached from {client.FirstName} {client.Name}");
    }
}
