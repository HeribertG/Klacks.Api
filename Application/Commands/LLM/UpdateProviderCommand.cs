using MediatR;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Application.Commands.LLM;

public class UpdateProviderCommand : IRequest<LLMProvider?>
{
    public Guid Id { get; set; }

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public bool IsEnabled { get; set; }

    public int Priority { get; set; }
}