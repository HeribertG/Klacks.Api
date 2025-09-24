using MediatR;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Queries.LLM;

public record GetLLMModelByModelIdQuery(string ModelId) : IRequest<LLMModel?>;