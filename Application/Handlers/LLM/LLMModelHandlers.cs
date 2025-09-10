using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;
using MediatR;

namespace Klacks.Api.Application.Handlers.LLM;

// Standard CRUD Handlers die BaseHandler erweitern
public class GetLLMModelQueryHandler : BaseHandler, IRequestHandler<GetQuery<LLMModel>, LLMModel>
{
    private readonly ILLMRepository _repository;

    public GetLLMModelQueryHandler(ILLMRepository repository, ILogger<GetLLMModelQueryHandler> logger) : base(logger)
    {
        _repository = repository;
    }

    public async Task<LLMModel> Handle(GetQuery<LLMModel> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(() =>
        {
            // Verwende BaseRepository Get-Methode
            return _repository.Get(request.Id);
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
            // Verwende spezialisierte Repository-Methode für Models mit Provider-Info
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
            // Validierung
            if (string.IsNullOrEmpty(request.Resource.ModelId))
            {
                throw new ArgumentException("ModelId ist erforderlich");
            }

            // Prüfe auf Duplikate
            var existing = await _repository.GetModelByIdAsync(request.Resource.ModelId);
            if (existing != null)
            {
                throw new InvalidOperationException($"Model mit ID {request.Resource.ModelId} existiert bereits");
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
            // Prüfe ob Model existiert
            var existing = await _repository.Get(request.Resource.Id);
            if (existing == null)
            {
                throw new KeyNotFoundException($"LLM Model mit ID {request.Resource.Id} nicht gefunden");
            }

            // Update relevante Felder
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
                throw new KeyNotFoundException($"LLM Model mit ID {request.Id} nicht gefunden");
            }

            // Prüfe ob es das Default-Model ist
            if (model.IsDefault)
            {
                throw new InvalidOperationException("Das Standard-Modell kann nicht gelöscht werden");
            }

            // Soft Delete über BaseRepository (nutzt BaseEntity.IsDeleted)
            return await _repository.Delete(request.Id);
        }, "DeleteLLMModel", request.Id);
    }
}