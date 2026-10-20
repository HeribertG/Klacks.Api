// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public class LLMStreamRequest
{
    public string Message { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string? ConversationId { get; set; }
    public string? ModelId { get; set; }
    public string? Language { get; set; }
    public List<string> UserRights { get; set; } = new();

    public BearerToken? AccessToken { get; set; }
    public AssistantPageContext? PageContext { get; set; }
    public bool IsVoiceMode { get; set; }

    /// <summary>
    /// Id this turn is logged and stored under. Supplied by the caller so it can publish the same id
    /// as the ambient TurnCorrelation from ITS own async flow: an AsyncLocal written inside an async
    /// iterator is not guaranteed to survive a yield back to the consumer, so the value has to be set
    /// where the enumeration is driven from. Empty means "generate one here".
    /// </summary>
    public Guid TurnId { get; set; }

    /// <summary>
    /// Cancelled when the turn's owner asks for a stop. Distinct from the HTTP request token on purpose: a
    /// stop lets the turn finish cleanly and persist what happened, a disconnect does not. None when the
    /// turn is not registered for stop requests.
    /// </summary>
    public CancellationToken StopToken { get; set; }

    /// <summary>
    /// Stopwatch timestamp the turn's elapsed times are measured from. Supplied by the caller so the
    /// status events count from when the request arrived rather than from when this orchestrator is
    /// reached - the caller has already normalized, matched and logged by then. Null means "start the
    /// clock here".
    /// </summary>
    public long? TurnStartTimestamp { get; set; }
}
