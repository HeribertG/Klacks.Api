// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes an identity provider via the identity-provider DeleteCommand. Users who
/// authenticate through the deleted provider lose their login path, so this action must be
/// confirmed deliberately. The response contains only non-sensitive fields.
/// </summary>
/// <param name="id">Required. UUID of the identity provider to delete (find it via list_identity_providers).</param>

using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_identity_provider")]
public class DeleteIdentityProviderSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteIdentityProviderSkill(IMediator mediator)
    {
        _mediator = mediator;
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

        var deleted = await _mediator.Send(new DeleteCommand(id), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Identity provider '{id}' not found.");
        }

        var lockoutNote = deleted.UseForAuthentication
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
            $"Identity provider '{deleted.Name}' ({deleted.Type}) deleted.{lockoutNote}");
    }
}
