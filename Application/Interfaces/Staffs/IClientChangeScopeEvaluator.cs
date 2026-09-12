// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Staffs;

/// <summary>
/// Decides how far an incoming client update reaches: only the annotations, or beyond them. The note
/// card of edit-address saves through the client aggregate (PUT api/backend/Clients), so a caller who
/// may write notes but not clients has to be let through that endpoint — this evaluator is the only
/// thing that keeps such a caller from rewriting every other field of the client in the same request.
/// It is deliberately strict: anything it cannot prove to be unchanged counts as a change.
/// </summary>
public interface IClientChangeScopeEvaluator
{
    /// <param name="incoming">The client resource as the caller sent it</param>
    /// <param name="cancellationToken">Cancels the read of the stored client</param>
    Task<ClientChangeScope> EvaluateAsync(ClientResource incoming, CancellationToken cancellationToken = default);
}
