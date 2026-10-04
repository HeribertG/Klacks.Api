// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a dispatch row's content may be re-rendered from the live payload of its condition-ledger
/// row (ProactiveContentParamMerge). The ledger keeps ONE row per finding, and for a client aggregate kind
/// that row's payload names every affected employee across the whole installation. A supervisor's dispatch
/// row was narrowed to the employees that supervisor may see, so merging the live payload over it would
/// put back exactly the names and count the narrowing withheld. Admins see every employee and keep the live
/// merge; everyone else keeps the parameters frozen onto their own row. Shared by the inbox read and the
/// reminder sweep so the two cannot disagree.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ProactiveLivePayloadPolicy
{
    /// <param name="triggerKind">The dispatch row's trigger kind.</param>
    /// <param name="recipientIsAdmin">Whether the row's recipient currently holds the Admin role.</param>
    public static bool MayMergeLivePayload(string triggerKind, bool recipientIsAdmin) =>
        recipientIsAdmin || !IsClientAggregateKind(triggerKind);

    public static bool IsClientAggregateKind(string triggerKind) =>
        AgentTriggerClientAggregateKinds.Values.Contains(triggerKind, StringComparer.Ordinal);
}
