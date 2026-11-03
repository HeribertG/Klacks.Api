// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Defaults of a new WorkChange. ToInvoice is the single owner rule "a WorkChange is billable to the customer unless a planner,
/// supervisor or admin switches it off in the dialog"; the entity, the resource, the container expansion and the chat skills all take it from here.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class WorkChangeDefaults
{
    public const bool ToInvoice = true;
}
