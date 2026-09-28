// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for listing issued Klacks bot tokens. Never returns the token hash or plaintext --
/// only the display prefix, matching the personal-access-token convention.
/// </summary>
/// <param name="request">No parameters -- lists all bot tokens</param>

using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Application.Queries.Bots;
using Klacks.Api.Domain.Interfaces.Bots;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Bots;

public class GetKlacksBotTokensQueryHandler : IRequestHandler<GetKlacksBotTokensQuery, List<KlacksBotTokenListItemDto>>
{
    private readonly IKlacksBotTokenRepository _tokenRepository;

    public GetKlacksBotTokensQueryHandler(IKlacksBotTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<List<KlacksBotTokenListItemDto>> Handle(GetKlacksBotTokensQuery request, CancellationToken cancellationToken)
    {
        var tokens = await _tokenRepository.GetAllAsync(cancellationToken);

        return tokens
            .Select(t => new KlacksBotTokenListItemDto(t.Id, t.Name, t.TokenPrefix, t.ExpiresAt, t.LastUsedAt))
            .ToList();
    }
}
