// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Body of PUT /phrases/{id}. Exactly one of the two fields is used, chosen by the row's Source: Phrase
/// for a learned phrase, Description for a proposed description change.
/// </summary>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public sealed record UpdateLearnedPhraseRequest(string? Phrase, string? Description);
