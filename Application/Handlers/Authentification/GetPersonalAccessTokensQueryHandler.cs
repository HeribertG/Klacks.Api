// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing the personal access tokens of a user. Returns metadata only,
/// never the token hash or plaintext.
/// </summary>
/// <param name="request">Contains the owner user id</param>

using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Application.Queries.Authentification;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Authentification;

public class GetPersonalAccessTokensQueryHandler : IRequestHandler<GetPersonalAccessTokensQuery, List<PersonalAccessTokenListItemDto>>
{
    private readonly IPersonalAccessTokenRepository _repository;

    public GetPersonalAccessTokensQueryHandler(IPersonalAccessTokenRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<PersonalAccessTokenListItemDto>> Handle(GetPersonalAccessTokensQuery request, CancellationToken cancellationToken)
    {
        var tokens = await _repository.GetByUserAsync(request.UserId, cancellationToken);

        return tokens
            .Select(token => new PersonalAccessTokenListItemDto(
                token.Id,
                token.Name,
                token.TokenPrefix,
                token.CreateTime,
                token.ExpiresAt,
                token.LastUsedAt))
            .ToList();
    }
}
