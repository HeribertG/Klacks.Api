// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Commands.Assistant;

public class UpdateProviderCommand : IRequest<LLMProvider?>
{
    public Guid Id { get; set; }

    public string? ProviderName { get; set; }

    public string? ApiKey { get; set; }

    public bool RequiresApiKey { get; set; } = true;

    public string? BaseUrl { get; set; }

    public string? ApiVersion { get; set; }

    public bool IsEnabled { get; set; }

    public int Priority { get; set; }
}