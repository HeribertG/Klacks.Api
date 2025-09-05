using Klacks.Api.Presentation.DTOs.LLM;

namespace Klacks.Api.Domain.Interfaces;

public interface ILLMService
{
    Task<LLMResponse> ProcessAsync(LLMContext context);
}