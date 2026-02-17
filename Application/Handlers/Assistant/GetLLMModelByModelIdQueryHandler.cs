using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Interfaces;

using Klacks.Api.Application.Queries.Assistant;
namespace Klacks.Api.Application.Handlers.Assistant;

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