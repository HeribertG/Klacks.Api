// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds transcription dictionary context from static entries and auto-imported master data.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IDictionaryService
{
    Task<string> BuildContextAsync(CancellationToken ct = default);
}
