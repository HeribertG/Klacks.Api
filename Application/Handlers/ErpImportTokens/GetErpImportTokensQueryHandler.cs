// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for listing the ERP import tokens issued for a drop point. Never returns the token
/// hash or plaintext -- only the display prefix, matching the personal-access-token convention.
/// </summary>
/// <param name="request">Contains the drop point whose tokens should be listed</param>

using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Queries.ErpImportTokens;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpImportTokens;

public class GetErpImportTokensQueryHandler : IRequestHandler<GetErpImportTokensQuery, List<ErpImportTokenListItemDto>>
{
    private readonly IErpImportTokenRepository _tokenRepository;

    public GetErpImportTokensQueryHandler(IErpImportTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<List<ErpImportTokenListItemDto>> Handle(GetErpImportTokensQuery request, CancellationToken cancellationToken)
    {
        var tokens = await _tokenRepository.GetByDropPointAsync(request.DropPointId, cancellationToken);

        return tokens
            .Select(t => new ErpImportTokenListItemDto(t.Id, t.DropPointId, t.Name, t.TokenPrefix, t.ExpiresAt, t.LastUsedAt))
            .ToList();
    }
}
