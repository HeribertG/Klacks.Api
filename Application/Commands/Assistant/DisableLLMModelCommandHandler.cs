// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

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
