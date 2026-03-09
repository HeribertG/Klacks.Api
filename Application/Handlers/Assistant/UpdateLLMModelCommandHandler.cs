// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpdateLLMModelCommandHandler : BaseTransactionHandler, IRequestHandler<PutCommand<LLMModel>, LLMModel?>
{
    private readonly ILLMRepository _repository;

    public UpdateLLMModelCommandHandler(ILLMRepository repository, IUnitOfWork unitOfWork, ILogger<UpdateLLMModelCommandHandler> logger) : base(unitOfWork, logger)
    {
        _repository = repository;
    }

    public async Task<LLMModel?> Handle(PutCommand<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var existing = await _repository.Get(request.Resource.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"LLM Model with ID {request.Resource.Id} not found");
            }

            if (request.Resource.IsDefault && !existing.IsDefault)
            {
                await _repository.SetDefaultModelAsync(existing.ModelId);
            }

            existing.ModelName = request.Resource.ModelName;
            existing.IsEnabled = request.Resource.IsEnabled;
            existing.IsDefault = request.Resource.IsDefault;
            existing.CostPerInputToken = request.Resource.CostPerInputToken;
            existing.CostPerOutputToken = request.Resource.CostPerOutputToken;
            existing.MaxTokens = request.Resource.MaxTokens;
            existing.ContextWindow = request.Resource.ContextWindow;
            existing.Description = request.Resource.Description;
            existing.Category = request.Resource.Category;

            return await _repository.UpdateModelAsync(existing);
        }, "UpdateLLMModel", request.Resource);
    }
}
