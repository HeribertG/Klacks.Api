// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class DeactivateGlobalRuleCommand : IRequest<Unit>
{
    public string Name { get; set; } = string.Empty;
}
