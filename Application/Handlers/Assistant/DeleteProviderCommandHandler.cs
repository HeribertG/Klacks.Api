// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeleteProviderCommandHandler : BaseTransactionHandler, IRequestHandler<DeleteProviderCommand, bool>
{
    private readonly ILLMRepository _repository;

    public DeleteProviderCommandHandler(ILLMRepository repository, IUnitOfWork unitOfWork, ILogger<DeleteProviderCommandHandler> logger) 
        : base(unitOfWork, logger)
    {
        _repository = repository;
    }

    public async Task<bool> Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var provider = await _repository.GetProviderAsync(request.Id);
            
            if (provider == null)
            {
                throw new KeyNotFoundException($"LLM Provider with ID {request.Id} not found");
            }

            var defaultModel = await _repository.GetDefaultModelAsync();
            
            if (defaultModel != null && defaultModel.ProviderId == provider.ProviderId)
            {
                throw new InvalidRequestException("Cannot delete provider of default model");
            }

            return await _repository.DeleteProviderAsync(request.Id);
        }, "DeleteLLMProvider", request.Id);
    }
}