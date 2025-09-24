using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Queries.LLM;

public record GetLLMModelsQuery() : ListQuery<LLMModel>();