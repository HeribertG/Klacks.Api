// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Revokes one of the requesting user's OWN personal access tokens so it can no longer
/// authenticate against the Klacks API. Strictly self-scoped: the revoke command always
/// carries the execution context's user id, so other users' tokens cannot be revoked.
/// The revoke is verified by re-reading the user's token list afterwards.
/// </summary>
/// <param name="tokenId">Required. UUID of the token (from list_personal_access_tokens).</param>

using Klacks.Api.Application.Commands.Authentification;
using Klacks.Api.Application.Queries.Authentification;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("revoke_personal_access_token")]
public class RevokePersonalAccessTokenSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public RevokePersonalAccessTokenSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var tokenId = GetRequiredGuid(parameters, "tokenId");
        var userId = context.UserId.ToString();

        var tokens = await _mediator.Send(new GetPersonalAccessTokensQuery(userId), cancellationToken);
        var token = tokens.FirstOrDefault(t => t.Id == tokenId);
        if (token == null)
        {
            return SkillResult.Error(
                $"Token '{tokenId}' was not found among your personal access tokens. " +
                "Use list_personal_access_tokens to see your tokens — other users' tokens cannot be revoked here.");
        }

        var success = await _mediator.Send(
            new RevokePersonalAccessTokenCommand(tokenId, userId), cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Revoking token '{token.Name}' failed.");
        }

        var remaining = await _mediator.Send(new GetPersonalAccessTokensQuery(userId), cancellationToken);
        if (remaining.Any(t => t.Id == tokenId))
        {
            return SkillResult.Error(
                $"Database verification failed: token '{token.Name}' is still listed after the revoke — " +
                "treat the revoke as not persisted.");
        }

        return SkillResult.SuccessResult(
            new { TokenId = tokenId, token.Name, token.TokenPrefix },
            $"Personal access token '{token.Name}' ({token.TokenPrefix}…) was revoked and confirmed in the " +
            "database (verified). Clients using it can no longer authenticate.");
    }
}
