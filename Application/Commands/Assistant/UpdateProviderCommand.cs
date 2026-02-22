// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Commands.Assistant;

public class UpdateProviderCommand : IRequest<LLMProvider?>
{
    public Guid Id { get; set; }

    public string? ProviderName { get; set; }

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public string? ApiVersion { get; set; }

    public bool IsEnabled { get; set; }

    public int Priority { get; set; }
}