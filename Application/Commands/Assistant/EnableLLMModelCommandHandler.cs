// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

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
