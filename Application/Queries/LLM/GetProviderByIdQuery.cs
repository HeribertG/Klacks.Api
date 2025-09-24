using MediatR;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Queries.LLM;

public class GetProviderByIdQuery : IRequest<LLMProvider?>
{
    public Guid Id { get; set; }

    public GetProviderByIdQuery(Guid id)
    {
        Id = id;
    }
}