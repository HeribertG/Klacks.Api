// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetLLMModelQueryHandler : BaseHandler, IRequestHandler<GetQuery<LLMModel>, LLMModel>
{
    private readonly ILLMRepository _repository;

    public GetLLMModelQueryHandler(ILLMRepository repository, ILogger<GetLLMModelQueryHandler> logger) : base(logger)
    {
        _repository = repository;
    }

    public async Task<LLMModel> Handle(GetQuery<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var model = await _repository.Get(request.Id);
            if (model == null)
            {
                throw new KeyNotFoundException($"LLM Model with ID {request.Id} not found");
            }
            return model;
        }, "GetLLMModel", request.Id);
    }
}

public class ListLLMModelsQueryHandler : BaseHandler, IRequestHandler<ListQuery<LLMModel>, IEnumerable<LLMModel>>
{
    private readonly ILLMRepository _repository;

    public ListLLMModelsQueryHandler(ILLMRepository repository, ILogger<ListLLMModelsQueryHandler> logger) : base(logger)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<LLMModel>> Handle(ListQuery<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() =>
        {
            return _repository.GetModelsAsync(onlyEnabled: false);
        }, "ListLLMModels");
    }
}

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