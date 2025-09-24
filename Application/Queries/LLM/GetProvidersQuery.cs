using MediatR;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Queries.LLM;

public class GetProvidersQuery : IRequest<List<LLMProvider>>
{
}