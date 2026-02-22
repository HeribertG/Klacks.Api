// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Commands.Assistant;

public record EnableLLMModelCommand(string ModelId) : IRequest<LLMModel>;
public record DisableLLMModelCommand(string ModelId) : IRequest<LLMModel>;
public record SetDefaultLLMModelCommand(string ModelId) : IRequest<LLMModel>;

public class EnableLLMModelCommandHandler : IRequestHandler<EnableLLMModelCommand, LLMModel>
{
    private readonly ILLMRepository _repository;

    public EnableLLMModelCommandHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<LLMModel> Handle(EnableLLMModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _repository.GetModelByIdAsync(request.ModelId);
        if (model == null)
        {
            throw new ArgumentException($"Model {request.ModelId} not found");
        }

        model.IsEnabled = true;
        return await _repository.UpdateModelAsync(model);
    }
}

public class DisableLLMModelCommandHandler : IRequestHandler<DisableLLMModelCommand, LLMModel>
{
    private readonly ILLMRepository _repository;

    public DisableLLMModelCommandHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<LLMModel> Handle(DisableLLMModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _repository.GetModelByIdAsync(request.ModelId);
        if (model == null)
        {
            throw new ArgumentException($"Model {request.ModelId} not found");
        }

        if (model.IsDefault)
        {
            throw new InvalidOperationException("Default model cannot be disabled");
        }

        model.IsEnabled = false;
        return await _repository.UpdateModelAsync(model);
    }
}

public class SetDefaultLLMModelCommandHandler : IRequestHandler<SetDefaultLLMModelCommand, LLMModel>
{
    private readonly ILLMRepository _repository;

    public SetDefaultLLMModelCommandHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<LLMModel> Handle(SetDefaultLLMModelCommand request, CancellationToken cancellationToken)
    {
        var model = await _repository.GetModelByIdAsync(request.ModelId);
        if (model == null)
        {
            throw new ArgumentException($"Model {request.ModelId} not found");
        }

        if (!model.IsEnabled)
        {
            throw new InvalidOperationException("Only enabled models can be set as default");
        }

        await _repository.SetDefaultModelAsync(request.ModelId);
        return await _repository.GetModelByIdAsync(request.ModelId) ?? model;
    }
}