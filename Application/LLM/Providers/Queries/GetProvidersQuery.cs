using MediatR;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.LLM.Providers.Queries;

public class GetProvidersQuery : IRequest<List<LLMProvider>>
{
}