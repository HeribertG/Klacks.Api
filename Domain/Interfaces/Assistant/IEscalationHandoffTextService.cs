// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Renders the escalation handoff sentences (confirmation, quiet note, exhausted note) in the installation
/// language. The language is resolved when a text is rendered, and a language whose pack lacks a key falls
/// back to English instead of dropping the note.
/// </summary>
/// <param name="key">One of EscalationHandoffTexts.RequiredKeys</param>
/// <param name="parameters">Value per placeholder name, see EscalationHandoffPlaceholders</param>
/// <param name="cancellationToken">Cancels the language lookup</param>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IEscalationHandoffTextService
{
    Task<string> RenderAsync(
        string key, IReadOnlyDictionary<string, string> parameters, CancellationToken cancellationToken = default);
}
