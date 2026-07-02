// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for revoking an ERP import token, scoped to its owning drop point so a token cannot
/// be revoked (or its existence probed) via a mismatched drop point id.
/// </summary>
/// <param name="request">Contains the token id and the drop point it must belong to</param>

using Klacks.Api.Application.Commands.ErpImportTokens;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpImportTokens;

public class RevokeErpImportTokenCommandHandler : IRequestHandler<RevokeErpImportTokenCommand, bool>
{
    private readonly IErpImportTokenRepository _tokenRepository;

    public RevokeErpImportTokenCommandHandler(IErpImportTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<bool> Handle(RevokeErpImportTokenCommand request, CancellationToken cancellationToken)
    {
        var revoked = await _tokenRepository.RevokeAsync(request.Id, request.DropPointId, cancellationToken);
        return revoked != null;
    }
}
