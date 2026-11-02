// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The world a shift with an analyse token lives in. A Work may sit on such a shift only under the same token, and never on
/// a shift of a variant (key): a key carries shifts only, works land on its scenario clones. A shift without a token has no
/// ShiftWorld: it is real, and any Work may reference it (scenario works on shifts the scenario did not clone do so by design).
/// </summary>
/// <param name="ShiftId">Id of the shift</param>
/// <param name="AnalyseToken">Token the shift carries</param>
/// <param name="IsVariant">True when the token belongs to a variant (key) row, deleted or not</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ShiftWorld(Guid ShiftId, Guid AnalyseToken, bool IsVariant)
{
    public bool Admits(Guid? workToken) => !IsVariant && AnalyseToken == workToken;
}
