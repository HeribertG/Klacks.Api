// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for issuing a Klacks bot token. Validates name and expiry bounds, generates the
/// token and returns the plaintext exactly once together with the persisted metadata.
/// </summary>
/// <param name="request">Contains the display name and the optional expiry in days</param>

using Klacks.Api.Application.Commands.Bots;
using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Bots;
using Klacks.Api.Domain.Models.Bots;
using Klacks.Api.Domain.Security;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Bots;

public class CreateKlacksBotTokenCommandHandler : IRequestHandler<CreateKlacksBotTokenCommand, KlacksBotTokenCreatedDto>
{
    private readonly IKlacksBotTokenRepository _tokenRepository;

    public CreateKlacksBotTokenCommandHandler(IKlacksBotTokenRepository tokenRepository)
    {
        _tokenRepository = tokenRepository;
    }

    public async Task<KlacksBotTokenCreatedDto> Handle(CreateKlacksBotTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidRequestException("Token name must not be empty.");
        }

        var expiresInDays = request.ExpiresInDays ?? KlacksBotTokenConstants.DefaultExpiryDays;
        if (expiresInDays < KlacksBotTokenConstants.MinExpiryDays || expiresInDays > KlacksBotTokenConstants.MaxExpiryDays)
        {
            throw new InvalidRequestException(
                $"Token expiration must be between {KlacksBotTokenConstants.MinExpiryDays} and {KlacksBotTokenConstants.MaxExpiryDays} days.");
        }

        var (plaintext, tokenHash, tokenPrefix) = KlacksBotTokenGenerator.Generate();
        var expiresAt = DateTime.UtcNow.AddDays(expiresInDays);

        var token = new KlacksBotToken
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            ExpiresAt = expiresAt
        };

        await _tokenRepository.AddAsync(token, cancellationToken);

        return new KlacksBotTokenCreatedDto(token.Id, token.Name, token.TokenPrefix, expiresAt, plaintext);
    }
}
