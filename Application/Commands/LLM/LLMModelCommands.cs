using MediatR;
using Klacks.Api.Application.Commands;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Commands.LLM;

// Standard CRUD Commands
public record CreateLLMModelCommand(LLMModel Resource) : PostCommand<LLMModel>(Resource);
public record UpdateLLMModelCommand(LLMModel Resource) : PutCommand<LLMModel>(Resource);  
public record DeleteLLMModelCommand(Guid Id) : DeleteCommand<LLMModel>(Id);

// Spezielle LLM Model Commands
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
            throw new ArgumentException($"Model {request.ModelId} nicht gefunden");
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
            throw new ArgumentException($"Model {request.ModelId} nicht gefunden");
        }

        // Prüfe ob es das Default-Model ist
        if (model.IsDefault)
        {
            throw new InvalidOperationException("Das Standard-Modell kann nicht deaktiviert werden");
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
            throw new ArgumentException($"Model {request.ModelId} nicht gefunden");
        }

        // Model muss aktiviert sein
        if (!model.IsEnabled)
        {
            throw new InvalidOperationException("Nur aktivierte Modelle können als Standard gesetzt werden");
        }

        await _repository.SetDefaultModelAsync(request.ModelId);
        return await _repository.GetModelByIdAsync(request.ModelId) ?? model;
    }
}