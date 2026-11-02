// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shape rules for the Language field of the order export filters. Only the form of a culture tag
/// (e.g. de, en-US, zh-Hant-TW) is checked; unknown but well-formed codes fall back to the invariant
/// culture in the formatters. The length is capped by the ExportLog column the language is stored in.
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Validation.Exports;

internal static class ExportLanguageRules
{
    public const int MaxLanguageLength = ExportLogLimits.LanguageMaxLength;
    public const string LanguageTagPattern = "^[A-Za-z]{2,3}(-[A-Za-z0-9]{2,8})*$";
}
