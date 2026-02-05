using Klacks.Api.Application.DTOs.LLM;

namespace Klacks.Api.Domain.Interfaces.LLM;

public interface ILLMService
{
    Task<LLMResponse> ProcessAsync(LLMContext context);
}