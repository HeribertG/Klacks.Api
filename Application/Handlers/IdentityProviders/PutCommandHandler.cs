// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Handlers.IdentityProviders;

public class PutCommandHandler : IRequestHandler<PutCommand, IdentityProviderResource?>
{
    private readonly IIdentityProviderRepository _repository;
    private readonly IdentityProviderMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        IIdentityProviderRepository repository,
        IdentityProviderMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _repository = repository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IdentityProviderResource?> Handle(PutCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating identity provider: {Id}", request.Model.Id);

        var existingEntity = await _repository.Get(request.Model.Id);
        if (existingEntity == null)
        {
            _logger.LogWarning("Identity provider not found: {Id}", request.Model.Id);
            return null;
        }

        PreserveMaskedSecrets(request.Model, existingEntity);

        _mapper.UpdateEntity(request.Model, existingEntity);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Updated identity provider: {Id}", request.Model.Id);

        return _mapper.ToMaskedResource(existingEntity);
    }

    private static void PreserveMaskedSecrets(IdentityProviderResource model, IdentityProvider existingEntity)
    {
        if (model.BindPassword == SecretMask.Placeholder)
        {
            model.BindPassword = existingEntity.BindPassword;
        }

        if (model.ClientSecret == SecretMask.Placeholder)
        {
            model.ClientSecret = existingEntity.ClientSecret;
        }
    }
}
