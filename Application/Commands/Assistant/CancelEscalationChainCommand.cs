// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

/// <param name="Reason">Mandatory (Owner decision B7): a cancel is the stronger claim that nobody needs
/// to act, and the shift stays uncovered - the morning report must be able to explain why.</param>
public record CancelEscalationChainCommand(Guid ChainId, string UserId, string UserName, string Reason) : IRequest<HttpResultResource>;
