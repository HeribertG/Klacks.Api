// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strict parsing and formatting of the contract working-weekday list. The accepted tokens are exactly
/// Mon, Tue, Wed, Thu, Fri, Sat and Sun (comma separated, case-insensitive); anything else is rejected rather
/// than guessed, so a misheard weekday can never silently change a contract.
/// </summary>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractWorkdays
{
    public static bool TryParse(string? raw, out IReadOnlySet<DayOfWeek> days, out string? error)
    {
        var parsed = new HashSet<DayOfWeek>();
        days = parsed;
        error = null;

        foreach (var part in (raw ?? string.Empty).Split(ContractWorkdayTokens.Separator))
        {
            var token = part.Trim();
            var match = ContractWorkdayTokens.InCalendarOrder
                .FirstOrDefault(entry => string.Equals(entry.Token, token, StringComparison.OrdinalIgnoreCase));

            if (match.Token == null)
            {
                error = $"Unknown weekday '{token}' in '{ContractFieldNames.Workdays}'. " +
                        $"Use a comma separated list of {AllTokens()}.";
                return false;
            }

            parsed.Add(match.Day);
        }

        return true;
    }

    public static IReadOnlySet<DayOfWeek> Of(Contract contract) => FromFlags(
        contract.WorkOnMonday,
        contract.WorkOnTuesday,
        contract.WorkOnWednesday,
        contract.WorkOnThursday,
        contract.WorkOnFriday,
        contract.WorkOnSaturday,
        contract.WorkOnSunday);

    public static IReadOnlySet<DayOfWeek> Of(ContractResource contract) => FromFlags(
        contract.WorkOnMonday,
        contract.WorkOnTuesday,
        contract.WorkOnWednesday,
        contract.WorkOnThursday,
        contract.WorkOnFriday,
        contract.WorkOnSaturday,
        contract.WorkOnSunday);

    public static string Format(IReadOnlySet<DayOfWeek> days) => string.Join(
        ContractWorkdayTokens.Separator,
        ContractWorkdayTokens.InCalendarOrder.Where(entry => days.Contains(entry.Day)).Select(entry => entry.Token));

    public static void Apply(ContractResource contract, IReadOnlySet<DayOfWeek> days)
    {
        contract.WorkOnMonday = days.Contains(DayOfWeek.Monday);
        contract.WorkOnTuesday = days.Contains(DayOfWeek.Tuesday);
        contract.WorkOnWednesday = days.Contains(DayOfWeek.Wednesday);
        contract.WorkOnThursday = days.Contains(DayOfWeek.Thursday);
        contract.WorkOnFriday = days.Contains(DayOfWeek.Friday);
        contract.WorkOnSaturday = days.Contains(DayOfWeek.Saturday);
        contract.WorkOnSunday = days.Contains(DayOfWeek.Sunday);
    }

    public static string AllTokens() => string.Join(
        ContractWorkdayTokens.Separator,
        ContractWorkdayTokens.InCalendarOrder.Select(entry => entry.Token));

    private static IReadOnlySet<DayOfWeek> FromFlags(
        bool monday, bool tuesday, bool wednesday, bool thursday, bool friday, bool saturday, bool sunday)
    {
        var days = new HashSet<DayOfWeek>();
        AddIf(days, monday, DayOfWeek.Monday);
        AddIf(days, tuesday, DayOfWeek.Tuesday);
        AddIf(days, wednesday, DayOfWeek.Wednesday);
        AddIf(days, thursday, DayOfWeek.Thursday);
        AddIf(days, friday, DayOfWeek.Friday);
        AddIf(days, saturday, DayOfWeek.Saturday);
        AddIf(days, sunday, DayOfWeek.Sunday);
        return days;
    }

    private static void AddIf(HashSet<DayOfWeek> days, bool worked, DayOfWeek day)
    {
        if (worked)
        {
            days.Add(day);
        }
    }
}
