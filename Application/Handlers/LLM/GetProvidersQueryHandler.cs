using MediatR;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;

using Klacks.Api.Application.Queries.LLM;
namespace Klacks.Api.Application.Handlers.LLM;

public class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, List<LLMProvider>>
{
    private readonly ILLMRepository _repository;

    public GetProvidersQueryHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<LLMProvider>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetProvidersAsync();
    }
}