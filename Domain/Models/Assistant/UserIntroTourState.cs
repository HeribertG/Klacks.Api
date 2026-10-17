// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Intro-tour state of one user account (one row per AspNetUsers row, removed together with the account). Not a
/// soft-delete entity: the row has no meaning without its account.
/// </summary>
/// <param name="UserId">AspNetUsers id of the account, primary key</param>
/// <param name="Status">Stored lifecycle status, see IntroTourStatus</param>
/// <param name="CompletedStations">Station ids of the intro catalog the user has finished</param>
/// <param name="SnoozeCount">Number of explicit "later" answers so far</param>
/// <param name="LastOfferedAt">When the frontend last reported the offer as visible (UTC)</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class UserIntroTourState
{
    public string UserId { get; set; } = string.Empty;

    public string Status { get; set; } = IntroTourStatus.Pending;

    public List<string> CompletedStations { get; set; } = new();

    public int SnoozeCount { get; set; }

    public DateTime? LastOfferedAt { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }
}
