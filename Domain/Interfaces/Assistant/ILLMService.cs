using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILLMService
{
    Task<LLMResponse> ProcessAsync(LLMContext context);
}