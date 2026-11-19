// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Entries a directory search returned and whether the directory delivered all of them.
/// </summary>
/// <param name="Entries">The entries read, in directory order</param>
/// <param name="IsComplete">False when a size limit, a partial result or the caller's entry cap cut the list short</param>
public record LdapUserSearchResult(IReadOnlyList<LdapUserEntry> Entries, bool IsComplete);
