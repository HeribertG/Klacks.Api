// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client (employee, customer or extern employee) by id. Soft-delete sets
/// IsDeleted=true via DataBaseContext.OnBeforeSaving; historical works and contracts are
/// preserved. The delete goes to DELETE api/backend/Clients/{id}, so the same authorisation and
/// logging apply as to a delete from the browser. Reading the client first stays a direct query — it
/// only supplies the name for the confirmation message.
/// </summary>
/// <param name="clientId">Required. UUID of the client to delete.</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class DeleteClientSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_client";

    private readonly IClientRepository _clientRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;

    public DeleteClientSkill(
        IClientRepository clientRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes)
    {
        _clientRepository = clientRepository;
        _selfApi = selfApi;
        _routes = routes;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");

        var client = await _clientRepository.Get(clientId);
        if (client == null)
        {
            return SkillResult.Error($"Client with ID '{clientId}' not found.");
        }

        var displayName = $"{client.FirstName} {client.Name}".Trim();
        var entityType = client.Type.ToString();

        var result = await _selfApi.DeleteAsync<ClientResource>(
            $"{_routes.Resolve(typeof(ClientResource))}/{clientId}", context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                DeletedClientName = displayName,
                EntityType = entityType
            },
            $"Client '{displayName}' was soft-deleted.");
    }
}
