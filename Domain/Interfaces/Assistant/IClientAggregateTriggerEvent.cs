// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A client-scoped event that bundles SEVERAL employees into one finding (a name list plus a count). Its
/// condition-ledger row keeps the unnarrowed workforce-wide payload, so the live payload merge of the inbox
/// and the reminder sweep may only be applied for Admins; every other recipient keeps the narrowed
/// parameters frozen onto their own dispatch row. Its Kind must be listed in
/// AgentTriggerClientAggregateKinds.Values, which the read paths consult (guarded by
/// AgentTriggerClientAggregateKindsGuardTests).
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IClientAggregateTriggerEvent : IClientScopedTriggerEvent
{
}
