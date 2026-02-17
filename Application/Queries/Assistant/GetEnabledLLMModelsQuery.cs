using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public record GetEnabledLLMModelsQuery(bool OnlyEnabled) : IRequest<List<LLMModel>>;