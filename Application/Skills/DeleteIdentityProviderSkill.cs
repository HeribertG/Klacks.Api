// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an identity provider via the identity-provider DeleteCommand, inside a transaction
/// that re-reads the row afterwards and rolls back if it is still visible. Users who authenticate
/// through the deleted provider lose their login path, so this action must be confirmed
/// deliberately. The response contains only non-sensitive fields.
/// </summary>
/// <param name="id">Required. UUID of the identity provider to delete (find it via list_identity_providers).</param>

using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_identity_provider")]
public class DeleteIdentityProviderSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_identity_provider";

    private readonly IMediator _mediator;
    private readonly IIdentityProviderRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteIdentityProviderSkill(
        IMediator mediator,
        IIdentityProviderRepository repository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var idRaw = GetParameter<string>(parameters, "id");
        if (string.IsNullOrWhiteSpace(idRaw) || !Guid.TryParse(idRaw, out var id))
        {
            return SkillResult.Error("Missing or invalid required parameter 'id'. Use list_identity_providers to find the provider id.");
        }

        IdentityProviderResource? deleted = null;

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                deleted = await _mediator.Send(new DeleteCommand(id), cancellationToken);
                if (deleted == null)
                {
                    throw new SkillVerificationException(SkillName, $"Identity provider '{id}' not found.");
                }

                await ConfirmDeletedAsync(
                    SkillName,
                    () => _repository.GetNoTracking(id),
                    $"the identity provider '{deleted.Name}'");

                return deleted.Id;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var lockoutNote = deleted!.UseForAuthentication
            ? " Warning: this provider was used for authentication — users who signed in through it can no longer log in."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                deleted.Id,
                deleted.Name,
                Type = deleted.Type.ToString(),
                WasEnabled = deleted.IsEnabled,
                deleted.UseForAuthentication,
                deleted.UseForClientImport
            },
            $"Identity provider '{deleted.Name}' ({deleted.Type}) deleted and confirmed removed from the database (verified).{lockoutNote}");
    }
}
