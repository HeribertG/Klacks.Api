// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the requesting user's OWN personal access tokens (name, token prefix, creation,
/// expiry and last-use timestamps — never the token value itself). Use it to find the
/// token id before calling revoke_personal_access_token.
/// </summary>

using Klacks.Api.Application.Queries.Authentification;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_personal_access_tokens")]
public class ListPersonalAccessTokensSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListPersonalAccessTokensSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _mediator.Send(
            new GetPersonalAccessTokensQuery(context.UserId.ToString()), cancellationToken);

        var listed = tokens
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.TokenPrefix,
                CreatedAt = t.CreatedAt?.ToString("yyyy-MM-dd"),
                ExpiresAt = t.ExpiresAt?.ToString("yyyy-MM-dd"),
                LastUsedAt = t.LastUsedAt?.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = listed.Count, Tokens = listed },
            listed.Count == 0
                ? "You have no personal access tokens."
                : $"You have {listed.Count} personal access token(s). Token values are never shown again — " +
                  "only the prefix identifies them.");
    }
}
