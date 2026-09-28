// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Values of LearnedPhraseDto.Status. A learned phrase is always active - the card lists no other kind -
/// while a description row carries the proposal's own status verbatim, so an automatically applied change
/// is visibly different from one that is still open or that the regression gate withheld.
/// </summary>
namespace Klacks.Api.Application.DTOs.Assistant.Learning;

public static class LearnedPhraseStatuses
{
    public const string Active = "active";
}
