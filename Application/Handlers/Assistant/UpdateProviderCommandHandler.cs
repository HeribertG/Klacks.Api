using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand, LLMProvider?>
{
    private readonly ILLMRepository _repository;
    private readonly LLMMapper _lLMMapper;

    public UpdateProviderCommandHandler(ILLMRepository repository, LLMMapper lLMMapper)
    {
        _repository = repository;
        _lLMMapper = lLMMapper;
    }

    public async Task<LLMProvider?> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = await _repository.GetProviderAsync(request.Id);
        if (provider == null)
        {
            return null;
        }

        var isEnabledChanged = provider.IsEnabled != request.IsEnabled;

        if (!string.IsNullOrEmpty(request.ProviderName))
        {
            provider.ProviderName = request.ProviderName;
        }
        if (!string.IsNullOrEmpty(request.ApiKey))
        {
            provider.ApiKey = request.ApiKey;
        }
        if (!string.IsNullOrEmpty(request.BaseUrl))
        {
            provider.BaseUrl = request.BaseUrl;
        }
        if (!string.IsNullOrEmpty(request.ApiVersion))
        {
            provider.ApiVersion = request.ApiVersion;
        }
        provider.IsEnabled = request.IsEnabled;
        provider.Priority = request.Priority;

        var updatedProvider = await _repository.UpdateProviderAsync(provider);

        if (isEnabledChanged)
        {
            var models = await _repository.GetModelsAsync(onlyEnabled: false);
            var providerModels = models.Where(m => m.ProviderId == provider.ProviderId).ToList();
            foreach (var model in providerModels)
            {
                model.IsEnabled = request.IsEnabled;
                await _repository.UpdateModelAsync(model);
            }
        }

        return updatedProvider;
    }
}