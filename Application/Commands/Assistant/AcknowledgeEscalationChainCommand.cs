// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record AcknowledgeEscalationChainCommand(Guid ChainId, string UserId) : IRequest<HttpResultResource>;
