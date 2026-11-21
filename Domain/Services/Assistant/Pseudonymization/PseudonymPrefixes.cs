// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps each pseudonym kind to its language-neutral token prefix (MA-0017, KL-0003, ...) and back.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Pseudonymization;

public static class PseudonymPrefixes
{
    public const string Employee = "MA";
    public const string Customer = "KL";
    public const string User = "BE";
    public const string Name = "NAME";
    public const string Phone = "TEL";
    public const string Mail = "MAIL";
    public const string Address = "ADR";

    private static readonly IReadOnlyDictionary<PseudonymKind, string> ByKind = new Dictionary<PseudonymKind, string>
    {
        [PseudonymKind.Employee] = Employee,
        [PseudonymKind.Customer] = Customer,
        [PseudonymKind.User] = User,
        [PseudonymKind.Name] = Name,
        [PseudonymKind.Phone] = Phone,
        [PseudonymKind.Mail] = Mail,
        [PseudonymKind.Address] = Address
    };

    private static readonly IReadOnlyDictionary<string, PseudonymKind> ByPrefix =
        ByKind.ToDictionary(pair => pair.Value, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    public static IEnumerable<string> All => ByKind.Values;
    public static string Of(PseudonymKind kind) => ByKind[kind];

    public static bool TryGetKind(string prefix, out PseudonymKind kind) => ByPrefix.TryGetValue(prefix, out kind);
}
