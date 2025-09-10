using MediatR;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Queries.LLM;

public record GetLLMModelsQuery() : ListQuery<LLMModel>();

public record GetEnabledLLMModelsQuery(bool OnlyEnabled) : IRequest<List<LLMModel>>;

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