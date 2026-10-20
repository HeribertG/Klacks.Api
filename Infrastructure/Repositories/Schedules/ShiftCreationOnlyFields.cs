// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The ERP reference and the scenario clone tracking are written when the row is created (ERP import,
/// order supersession, scenario clone) and ShiftResource does not carry them, so a shift mapped from the
/// edit form or the cut dialog brings them as null. An update keeps the stored values instead of
/// overwriting them; the internal callers that pass a loaded entity hold the same values anyway.
/// </summary>
/// <param name="stored">The tracked shift as stored.</param>
/// <param name="incoming">The shift whose values are about to be copied onto the stored one.</param>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftCreationOnlyFields
{
    public static void KeepStored(Shift stored, Shift incoming)
    {
        incoming.SourceSystemId = stored.SourceSystemId;
        incoming.ExternalOrderReference = stored.ExternalOrderReference;
        incoming.SupersedesOrderId = stored.SupersedesOrderId;
        incoming.ScenarioSourceShiftId = stored.ScenarioSourceShiftId;
        incoming.SourceChildCountSnapshot = stored.SourceChildCountSnapshot;
    }
}
