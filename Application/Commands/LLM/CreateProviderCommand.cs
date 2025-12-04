using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Commands.LLM;

public class CreateProviderCommand : IRequest<LLMProvider>
{
    public string ProviderId { get; set; } = string.Empty;

    public string ProviderName { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public bool IsEnabled { get; set; } = true;

    public int Priority { get; set; } = 10;
}