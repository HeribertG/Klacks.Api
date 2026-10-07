// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Picks the number a planner should dial to reach an employee about a replacement: a mobile number before a
/// fixed line, private before office within each kind. The emergency number belongs to somebody else and is
/// never offered. The country prefix is put in front of the number when one is stored.
/// </summary>
/// <param name="communications">Contact entries of one employee</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Schedules;

public static class PreferredPhoneSelector
{
    public static readonly IReadOnlyList<CommunicationTypeEnum> PhoneTypesByPreference =
    [
        CommunicationTypeEnum.PrivateCellPhone,
        CommunicationTypeEnum.OfficeCellPhone,
        CommunicationTypeEnum.PrivateFixPhone,
        CommunicationTypeEnum.OfficeFixPhone,
    ];

    public static string? Select(IEnumerable<Communication> communications)
    {
        var best = communications
            .Where(c => !string.IsNullOrWhiteSpace(c.Value) && PhoneTypesByPreference.Contains(c.Type))
            .OrderBy(c => IndexOf(c.Type))
            .FirstOrDefault();

        return best is null ? null : Format(best);
    }

    private static int IndexOf(CommunicationTypeEnum type)
    {
        for (var i = 0; i < PhoneTypesByPreference.Count; i++)
        {
            if (PhoneTypesByPreference[i] == type)
            {
                return i;
            }
        }

        return PhoneTypesByPreference.Count;
    }

    private static string Format(Communication communication)
    {
        var value = communication.Value.Trim();
        var prefix = communication.Prefix?.Trim() ?? string.Empty;

        return prefix.Length == 0 || value.StartsWith(prefix, StringComparison.Ordinal)
            ? value
            : $"{prefix} {value}";
    }
}
