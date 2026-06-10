// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists all configured identity providers (LDAP, Active Directory, OAuth2, OpenID Connect)
/// via the identity-provider ListQuery. Returns only non-sensitive fields — bind passwords,
/// client secrets and other credentials are never exposed. Use this to find provider IDs
/// before update_identity_provider / delete_identity_provider.
/// </summary>

using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_identity_providers")]
public class ListIdentityProvidersSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListIdentityProvidersSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var providers = await _mediator.Send(new ListQuery(), cancellationToken);

        var projected = providers
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                Type = p.Type.ToString(),
                p.IsEnabled,
                p.SortOrder,
                p.UseForAuthentication,
                p.UseForClientImport,
                p.LastSyncTime,
                p.LastSyncCount,
                p.LastSyncError
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, Providers = projected },
            $"Found {projected.Count} identity providers. Secrets are never included in this listing.");
    }
}
