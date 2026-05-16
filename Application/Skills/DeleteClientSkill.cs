// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client (employee, customer or extern employee) by id. Soft-delete sets
/// IsDeleted=true via DataBaseContext.OnBeforeSaving; historical works and contracts are
/// preserved. Use restore_client (not yet implemented) to undo.
/// </summary>
/// <param name="clientId">Required. UUID of the client to delete.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_client")]
public class DeleteClientSkill : BaseSkillImplementation
{
    private readonly IClientRepository _clientRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteClientSkill(IClientRepository clientRepository, IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _unitOfWork = unitOfWork;
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

        await _clientRepository.Delete(clientId);
        await _unitOfWork.CompleteAsync();

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
