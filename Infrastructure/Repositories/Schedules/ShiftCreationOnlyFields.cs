// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The ERP reference, the scenario clone tracking and the variant copy reference are written when the row is created (ERP
/// import, order supersession, scenario clone, copy into a variant) and ShiftResource does not carry them, so a shift mapped from the
/// edit form or the cut dialog brings them as null. An update keeps the stored values instead of
/// overwriting them; the internal callers that pass a loaded entity hold the same values anyway. The same goes for the
/// analyse token: the mapper never sets it, so without this a saved shift of a variant (key) would lose its token and
/// become a real shift. The incoming group items take the stored token too, because they replace the stored ones by value
/// (EntityCollectionUpdateService), and a shift and its group items always carry the same token.
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
        incoming.CopiedFromShiftId = stored.CopiedFromShiftId;
        incoming.AnalyseToken = stored.AnalyseToken;
        foreach (var groupItem in incoming.GroupItems)
        {
            groupItem.AnalyseToken = stored.AnalyseToken;
        }
    }
}
