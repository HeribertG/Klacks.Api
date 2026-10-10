// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// How a contract NAME reference is matched. ExactOrUniquePartial is for skills that write: the Sensitive
/// confirmation is given on the model's arguments before resolution, so a fuzzy hit could change a different
/// contract than the one the user confirmed; only an exact name (case-insensitive) or a unique partial name
/// resolves. WithFuzzy adds the typo and phonetic stages and is for read-only skills.
/// </summary>

namespace Klacks.Api.Application.Services.Contracts;

public enum ContractNameMatchMode
{
    ExactOrUniquePartial = 1,
    WithFuzzy = 2
}
