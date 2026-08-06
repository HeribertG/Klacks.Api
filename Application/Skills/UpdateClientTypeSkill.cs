// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes which category a client is classified as (Employee, ExternEmp or Customer) and verifies
/// the new value against the database after saving.
/// </summary>
/// <param name="clientId">Optional. UUID of the client to update.</param>
/// <param name="firstName">Optional. First name of the client, used together with lastName when clientId is not supplied.</param>
/// <param name="lastName">Optional. Last name of the client, used together with firstName when clientId is not supplied.</param>
/// <param name="targetType">Required. New type: Employee, ExternEmp or Customer.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.Extensions.Logging;

using Klacks.Api.Application.DTOs.Staffs;

using Klacks.Api.Application.Mappers;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateClientTypeSkill : BaseSkillImplementation
{
    private const string SkillName = "update_client_type";

    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly ClientMapper _clientMapper;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ILogger<UpdateClientTypeSkill> _logger;

    public UpdateClientTypeSkill(
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        ClientMapper clientMapper,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ILogger<UpdateClientTypeSkill> logger)
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
        var targetTypeRaw = GetRequiredString(parameters, "targetType");
        if (!Enum.TryParse<EntityTypeEnum>(targetTypeRaw, true, out var targetType) || !Enum.IsDefined(targetType))
        {
            var validValues = string.Join(", ", Enum.GetNames<EntityTypeEnum>());
            return SkillResult.Error($"'{targetTypeRaw}' is not a valid client type. Available types: {validValues}.");
        }

        var (client, resolveError) = await ResolveClientAsync(parameters, cancellationToken);
        if (resolveError != null)
        {
            return SkillResult.Error(resolveError);
        }

        var clientId = client!.Id;

        if (client.Type == targetType)
        {
            return SkillResult.SuccessResult(
                new { ClientId = clientId, client.FirstName, LastName = client.Name, EntityType = client.Type.ToString() },
                $"{client.FirstName} {client.Name} is already classified as {targetType}.");
        }

        if (client.LegalEntity && targetType != EntityTypeEnum.Customer)
        {
            return SkillResult.Error(
                $"{client.FirstName} {client.Name} is registered as a legal entity and must stay classified as " +
                "Customer. The legal-entity flag has to change first if this client should become a different type.");
        }

        client.Type = targetType;

        var result = await _selfApi.PutAsync<ClientResource>(
            _routes.Resolve(typeof(ClientResource)), _clientMapper.ToResource(client), context,
            SkillName, cancellationToken);

        if (!result.Success)
        {
            _logger.LogWarning(
                "{SkillName} could not save the type change for client {ClientId}: {Reason}",
                SkillName, clientId, result.ErrorMessage);
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                client.FirstName,
                LastName = client.Name,
                EntityType = client.Type.ToString()
            },
            $"{client.FirstName} {client.Name} changed to {targetType}");
    }

    private async Task<(Client? Client, string? Error)> ResolveClientAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var clientIdValue = GetParameter<string>(parameters, "clientId");
        if (!string.IsNullOrWhiteSpace(clientIdValue))
        {
            if (!Guid.TryParse(clientIdValue, out var clientId))
            {
                return (null, $"'{clientIdValue}' is not a valid client id.");
            }

            var byId = await _clientRepository.Get(clientId);
            return byId == null
                ? (null, $"Client with ID '{clientId}' not found.")
                : (byId, null);
        }

        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return (null, "Provide either clientId or the client's name to identify the client.");
        }

        return await ClientResolver.ResolveByNameAsync(_searchRepository, _clientRepository, firstName, lastName, cancellationToken);
    }
}
