// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What a goldset-born description proposal was built from: the user messages shown to the optimizer and the
/// eval items behind them, so the gate can replay exactly those misses.
/// </summary>
/// <param name="Examples">The messages of the missed items</param>
/// <param name="Items">The missed items themselves</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoldsetMissEvidence(IReadOnlyList<string> Examples, IReadOnlyList<GoldsetItemRef> Items)
{
    public static GoldsetMissEvidence Empty { get; } = new([], []);
}
