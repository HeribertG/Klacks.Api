using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Handlers.LLM;

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

        var updatedProvider = _lLMMapper.ToProviderFromUpdate(request);
        provider.ApiKey = updatedProvider.ApiKey;
        provider.BaseUrl = updatedProvider.BaseUrl;

        return await _repository.UpdateProviderAsync(provider);
    }
}