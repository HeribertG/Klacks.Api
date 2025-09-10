using MediatR;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Queries.LLM;

public record GetLLMModelQuery(Guid Id) : GetQuery<LLMModel>(Id);

public record GetLLMModelByModelIdQuery(string ModelId) : IRequest<LLMModel?>;

public class GetLLMModelByModelIdQueryHandler : IRequestHandler<GetLLMModelByModelIdQuery, LLMModel?>
{
    private readonly ILLMRepository _repository;

    public GetLLMModelByModelIdQueryHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<LLMModel?> Handle(GetLLMModelByModelIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetModelByIdAsync(request.ModelId);
    }
}