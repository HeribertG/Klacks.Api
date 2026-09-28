// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Body of PUT /capabilities/{id}. The steps are deliberately not editable: they are what the execution
/// oracle verified, and editing them would invalidate that verdict without re-running it.
/// </summary>
/// <param name="Synonyms">Trigger phrases per language code, null leaves them untouched</param>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record UpdateLearnedCapabilityRequest(
    string? Goal,
    Dictionary<string, List<string>>? Synonyms);
