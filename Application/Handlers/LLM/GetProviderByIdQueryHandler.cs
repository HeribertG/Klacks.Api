using MediatR;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;

using Klacks.Api.Application.Queries.LLM;
namespace Klacks.Api.Application.Handlers.LLM;

public class GetProviderByIdQueryHandler : IRequestHandler<GetProviderByIdQuery, LLMProvider?>
{
    private readonly ILLMRepository _repository;

    public GetProviderByIdQueryHandler(ILLMRepository repository)
    {
        _repository = repository;
    }

    public async Task<LLMProvider?> Handle(GetProviderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetProviderAsync(request.Id);
    }
}