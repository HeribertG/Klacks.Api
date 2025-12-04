using Klacks.Api.Infrastructure.Mediator;
using AutoMapper;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Handlers.LLM;

public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, LLMProvider>
{
    private readonly ILLMRepository _repository;
    private readonly IMapper _mapper;

    public CreateProviderCommandHandler(ILLMRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<LLMProvider> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        var provider = _mapper.Map<LLMProvider>(request);
        return await _repository.CreateProviderAsync(provider);
    }
}