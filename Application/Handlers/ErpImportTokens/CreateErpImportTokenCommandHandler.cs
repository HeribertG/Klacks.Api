// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for issuing an ERP import token bound to a single drop point. Validates the drop
/// point exists, validates name and expiry bounds, generates the token and returns the
/// plaintext exactly once together with the persisted metadata.
/// </summary>
/// <param name="request">Contains the target drop point id, the display name and the optional expiry in days</param>

using Klacks.Api.Application.Commands.ErpImportTokens;
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Security;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpImportTokens;

public class CreateErpImportTokenCommandHandler : IRequestHandler<CreateErpImportTokenCommand, ErpImportTokenCreatedDto>
{
    private readonly IErpImportTokenRepository _tokenRepository;
    private readonly IErpDropPointRepository _dropPointRepository;

    public CreateErpImportTokenCommandHandler(IErpImportTokenRepository tokenRepository, IErpDropPointRepository dropPointRepository)
    {
        _tokenRepository = tokenRepository;
        _dropPointRepository = dropPointRepository;
    }

    public async Task<ErpImportTokenCreatedDto> Handle(CreateErpImportTokenCommand request, CancellationToken cancellationToken)
    {
        var dropPoint = await _dropPointRepository.Exists(request.DropPointId);
        if (!dropPoint)
        {
            throw new InvalidRequestException($"Drop point {request.DropPointId} does not exist.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidRequestException("Token name must not be empty.");
        }

        var expiresInDays = request.ExpiresInDays ?? ErpImportTokenConstants.DefaultExpiryDays;
        if (expiresInDays < ErpImportTokenConstants.MinExpiryDays || expiresInDays > ErpImportTokenConstants.MaxExpiryDays)
        {
            throw new InvalidRequestException(
                $"Token expiration must be between {ErpImportTokenConstants.MinExpiryDays} and {ErpImportTokenConstants.MaxExpiryDays} days.");
        }

        var (plaintext, tokenHash, tokenPrefix) = ErpImportTokenGenerator.Generate();
        var expiresAt = DateTime.UtcNow.AddDays(expiresInDays);

        var token = new ErpImportToken
        {
            Id = Guid.NewGuid(),
            DropPointId = request.DropPointId,
            Name = request.Name.Trim(),
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            ExpiresAt = expiresAt
        };

        await _tokenRepository.AddAsync(token, cancellationToken);

        return new ErpImportTokenCreatedDto(token.Id, token.DropPointId, token.Name, token.TokenPrefix, expiresAt, plaintext);
    }
}
