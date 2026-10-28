// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Playground;

namespace Klacks.Api.Domain.Interfaces.Playground;

/// <summary>
/// In-memory daily token budget of the Playground with three independent buckets (global, IP, device).
/// Each bucket blocks on its own; a request is refused as soon as any bucket that applies to it is used up.
/// </summary>
public interface IPlaygroundTokenBudget
{
    /// <summary>
    /// True when any bucket that applies to the visitor has no budget left. Cheap pre-check before a turn starts.
    /// </summary>
    /// <param name="visitor">The visitor of the current request; null for work outside a request (global bucket only)</param>
    bool IsExhausted(PlaygroundVisitor? visitor);

    /// <summary>
    /// Books the estimate into every applicable bucket in one atomic step, or books nothing and returns null
    /// when a bucket is already used up. The call that crosses the line is allowed and may overshoot once.
    /// </summary>
    /// <param name="visitor">The visitor of the current request; null for work outside a request (global bucket only)</param>
    /// <param name="estimatedTokens">Prompt estimate plus the requested maximum output tokens</param>
    PlaygroundBudgetReservation? TryReserve(PlaygroundVisitor? visitor, long estimatedTokens);

    /// <summary>
    /// Replaces the booked estimate by the usage the provider reported. Idempotent: the last reported value wins.
    /// A reservation that is never settled (abort, no usage reported) keeps its estimate.
    /// </summary>
    /// <param name="reservation">The handle returned by TryReserve</param>
    /// <param name="actualTokens">Tokens the provider reported for the call, cache reads weighted as configured</param>
    void Settle(PlaygroundBudgetReservation reservation, long actualTokens);

    /// <summary>
    /// Hands the whole booked estimate back. Only for a call the provider refused before producing any output and
    /// reported no usage for (for example an HTTP error such as insufficient balance); an aborted call keeps its estimate.
    /// Idempotent.
    /// </summary>
    /// <param name="reservation">The handle returned by TryReserve</param>
    void Release(PlaygroundBudgetReservation reservation);
}
