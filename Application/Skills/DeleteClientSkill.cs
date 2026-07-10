// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a client (employee, customer or extern employee) by id. Soft-delete sets
/// IsDeleted=true via DataBaseContext.OnBeforeSaving; historical works and contracts are
/// preserved. The delete is self-verifying: it runs in a transaction and the client must no
/// longer be readable through the IsDeleted-filtered query before success is reported.
/// </summary>
/// <param name="clientId">Required. UUID of the client to delete.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class DeleteClientSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_client";

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

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _clientRepository.Delete(clientId);
                await _unitOfWork.CompleteAsync();
                await ConfirmDeletedAsync(
                    SkillName,
                    () => _clientRepository.GetNoTracking(clientId),
                    $"client '{displayName}'");
                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                DeletedClientName = displayName,
                EntityType = entityType
            },
            $"Client '{displayName}' was soft-deleted and confirmed no longer visible in the database (verified).");
    }
}
