using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Handlers.LLM;

public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, LLMProvider>
{
    private readonly ILLMRepository _repository;
    private readonly LLMMapper _lLMMapper;

    public CreateProviderCommandHandler(ILLMRepository repository, LLMMapper lLMMapper)
    {
        _repository = repository;
        _lLMMapper = lLMMapper;
    }

    public async Task<LLMProvider> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = _lLMMapper.ToProviderFromCreate(request);
        return await _repository.CreateProviderAsync(provider);
    }
}