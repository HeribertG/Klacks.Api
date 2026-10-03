// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for creating a personal access token. Validates name and expiry bounds, generates
/// the token and returns the plaintext exactly once together with the persisted metadata. A token
/// without an explicit access mode is Read (secure by default); Write must be chosen on purpose.
/// </summary>
/// <param name="request">Contains the owner user id, the display name, the optional expiry in days and the optional access mode</param>

using Klacks.Api.Application.Commands.Authentification;
using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Security;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Authentification;

public class CreatePersonalAccessTokenCommandHandler : IRequestHandler<CreatePersonalAccessTokenCommand, PersonalAccessTokenCreatedDto>
{
    private readonly IPersonalAccessTokenRepository _repository;

    public CreatePersonalAccessTokenCommandHandler(IPersonalAccessTokenRepository repository)
    {
        _repository = repository;
    }

    public async Task<PersonalAccessTokenCreatedDto> Handle(CreatePersonalAccessTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidRequestException("Token name must not be empty.");
        }

        var expiresInDays = request.ExpiresInDays ?? PatConstants.DefaultExpiresInDays;
        if (expiresInDays < PatConstants.MinExpiresInDays || expiresInDays > PatConstants.MaxExpiresInDays)
        {
            throw new InvalidRequestException(
                $"Token expiration must be between {PatConstants.MinExpiresInDays} and {PatConstants.MaxExpiresInDays} days.");
        }

        var accessMode = request.AccessMode ?? PatConstants.DefaultAccessMode;
        if (!Enum.IsDefined(accessMode))
        {
            throw new InvalidRequestException(
                $"Token access mode must be '{PersonalAccessTokenAccessMode.Read}' or '{PersonalAccessTokenAccessMode.Write}'.");
        }

        var (plaintext, tokenHash, tokenPrefix) = PatTokenGenerator.Generate();
        var expiresAt = DateTime.UtcNow.AddDays(expiresInDays);

        var token = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name.Trim(),
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            ExpiresAt = expiresAt,
            AccessMode = accessMode
        };

        await _repository.AddAsync(token, cancellationToken);

        return new PersonalAccessTokenCreatedDto(token.Id, token.Name, token.TokenPrefix, expiresAt, plaintext, token.AccessMode);
    }
}
