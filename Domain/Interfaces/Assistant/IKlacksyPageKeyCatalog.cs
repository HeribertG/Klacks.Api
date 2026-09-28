// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only catalog of all Klacksy navigation page-keys.
/// Loaded once from klacksy-page-keys.generated.json which is produced by
/// the Klacks.Ui scanner from the single TypeScript source of truth.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface IKlacksyPageKeyCatalog
{
    KlacksyPageKeyEntry? GetByPageKey(string pageKey);

    IReadOnlyList<string> AllPageKeys { get; }

    IReadOnlyList<KlacksyPageKeyEntry> All { get; }
}
