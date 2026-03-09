// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeleteLLMModelCommandHandler : BaseTransactionHandler, IRequestHandler<DeleteCommand<LLMModel>, LLMModel?>
{
    private readonly ILLMRepository _repository;

    public DeleteLLMModelCommandHandler(ILLMRepository repository, IUnitOfWork unitOfWork, ILogger<DeleteLLMModelCommandHandler> logger) : base(unitOfWork, logger)
    {
        _repository = repository;
    }

    public async Task<LLMModel?> Handle(DeleteCommand<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var model = await _repository.Get(request.Id);
            if (model == null)
            {
                throw new KeyNotFoundException($"LLM Model with ID {request.Id} not found");
            }

            if (model.IsDefault)
            {
                throw new InvalidRequestException("Default model cannot be deleted");
            }

            return await _repository.Delete(request.Id);
        }, "DeleteLLMModel", request.Id);
    }
}
