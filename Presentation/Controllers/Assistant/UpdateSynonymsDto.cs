// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Presentation.Controllers.Assistant;

public sealed record UpdateSynonymsDto(string Locale, string[] Synonyms, string Status);
