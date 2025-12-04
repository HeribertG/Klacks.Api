using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Queries.LLM;

namespace Klacks.Api.Application.Handlers.LLM;

public class GetEnabledLLMModelsQueryHandler : IRequestHandler<GetEnabledLLMModelsQuery, List<LLMModel>>
{
    private readonly ILLMRepository _repository;

    public GetEnabledLLMModelsQueryHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<LLMModel>> Handle(GetEnabledLLMModelsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetModelsAsync(request.OnlyEnabled);
    }
}