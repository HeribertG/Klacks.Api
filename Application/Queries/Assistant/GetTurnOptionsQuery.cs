// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for the skills that were offered to the model in one captured chat turn, addressed by the raw
/// user message of that turn.
/// </summary>
/// <param name="UserId">Caller identity from the token; only the caller's own turn is readable</param>
/// <param name="UserMessage">Raw user message of the turn, hashed by the handler when no turn id is sent</param>
/// <param name="TurnId">Optional server-assigned id of the turn; when set it is the exact key and the hash is never used</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public class GetTurnOptionsQuery : IRequest<TurnOptionsResult>
{
    public string UserId { get; set; } = string.Empty;

    public string UserMessage { get; set; } = string.Empty;

    public Guid? TurnId { get; set; }
}
