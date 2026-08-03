// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs one Slack owner bridge cycle: reads new inbound Slack messages from the owner's registered
/// self-alias, feeds each one through Klacksy's normal LLM conversation, and sends the reply back to
/// the same Slack DM.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISlackOwnerBridgeService
{
    /// <summary>
    /// Runs a single bridge cycle end to end.
    /// </summary>
    /// <returns>Number of inbound messages processed in this cycle.</returns>
    Task<int> RunCycleAsync(CancellationToken cancellationToken = default);
}
