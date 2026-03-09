// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CreateLLMModelCommandHandler : BaseTransactionHandler, IRequestHandler<PostCommand<LLMModel>, LLMModel?>
{
    private readonly ILLMRepository _repository;

    public CreateLLMModelCommandHandler(ILLMRepository repository, IUnitOfWork unitOfWork, ILogger<CreateLLMModelCommandHandler> logger) : base(unitOfWork, logger)
    {
        _repository = repository;
    }

    public async Task<LLMModel?> Handle(PostCommand<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var existing = await _repository.GetModelByIdAsync(request.Resource.ModelId);
            if (existing != null)
            {
                throw new InvalidOperationException($"Model with ID {request.Resource.ModelId} already exists");
            }

            if (request.Resource.IsDefault)
            {
                await _repository.SetDefaultModelAsync(request.Resource.ModelId);
            }

            return await _repository.CreateModelAsync(request.Resource);
        }, "CreateLLMModel", request.Resource);
    }
}
