using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Queries.LLM;

public record GetLLMModelQuery(Guid Id) : GetQuery<LLMModel>(Id);