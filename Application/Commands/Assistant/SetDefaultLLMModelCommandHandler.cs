// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

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
