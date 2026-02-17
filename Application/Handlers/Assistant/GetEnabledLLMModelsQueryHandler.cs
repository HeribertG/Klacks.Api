using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.Queries.Assistant;

namespace Klacks.Api.Application.Handlers.Assistant;

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