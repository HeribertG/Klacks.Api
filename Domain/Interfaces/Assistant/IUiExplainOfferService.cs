// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The explain offers the UI shows on a signal whose meaning it knows (T5). Reads whether one topic may be offered to a
/// user and records the user's reaction to it. Both methods act on the given user only; the caller passes the id of
/// the signed-in user, never one taken from a request.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IUiExplainOfferService
{
    /// <summary>
    /// Whether the topic may be offered to the user. Fail-closed: an unknown topic or any failure yields "off".
    /// </summary>
    /// <param name="userId">Signed-in user</param>
    /// <param name="topicKey">Topic key (UiExplainOfferTopics)</param>
    /// <param name="cancellationToken">Cancels the lookups</param>
    Task<UiExplainOfferState> GetStateAsync(string userId, string topicKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one outcome (UiExplainOfferOutcomes) as a counter row and mutes the topic for the user when the dosage
    /// says so. Returns false and writes nothing for an unknown topic or outcome.
    /// </summary>
    /// <param name="userId">Signed-in user</param>
    /// <param name="topicKey">Topic key (UiExplainOfferTopics)</param>
    /// <param name="outcome">shown, accepted, later or declined</param>
    /// <param name="cancellationToken">Cancels the writes</param>
    Task<bool> RecordReactionAsync(string userId, string topicKey, string outcome, CancellationToken cancellationToken = default);
}
