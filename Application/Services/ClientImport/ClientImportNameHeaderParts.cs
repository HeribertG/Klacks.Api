// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Where the name words of a header start in its normalized text, or null when the header has none of
/// that kind: a first-name word ("Vorname"), a last-name word ("Nachname") or a generic name word
/// ("Name") that stands for the last name when it appears next to a first-name word.
/// </summary>
/// <param name="FirstNamePosition">Start of the first first-name word</param>
/// <param name="LastNamePosition">Start of the first last-name word</param>
/// <param name="GenericNamePosition">Start of the first generic name word</param>

namespace Klacks.Api.Application.Services.ClientImport;

public record ClientImportNameHeaderParts(int? FirstNamePosition, int? LastNamePosition, int? GenericNamePosition);
