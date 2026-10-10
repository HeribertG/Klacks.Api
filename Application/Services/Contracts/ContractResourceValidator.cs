// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Value rules for a contract resource that is about to be created by a skill. FindViolation holds the rules the
/// create_contract skill has always applied (no negative hours, percent or rates; minimum not above a positive
/// maximum; guaranteed hours inside the band; validUntil after validFrom) with their original wording, so both
/// creating skills reject the same input the same way. FindHandlerRejection mirrors what the contract POST
/// handler throws for, so a skill can return a clean error instead of an exception.
/// </summary>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractResourceValidator
{
    public static string? FindViolation(ContractResource resource)
    {
        var nullableValues = new (string Key, decimal? Value)[]
        {
            (ContractFieldNames.GuaranteedHours, resource.GuaranteedHours),
            (ContractFieldNames.Percent, resource.Percent),
            (ContractFieldNames.NightRate, resource.NightRate),
            (ContractFieldNames.HolidayRate, resource.HolidayRate),
            (ContractFieldNames.SaRate, resource.WE1Rate),
            (ContractFieldNames.SoRate, resource.WE2Rate)
        };

        foreach (var (key, value) in nullableValues)
        {
            if (value is < decimal.Zero)
            {
                return NegativeMessage(key);
            }
        }

        var values = new (string Key, decimal Value)[]
        {
            (ContractFieldNames.MinimumHours, resource.MinimumHours),
            (ContractFieldNames.MaximumHours, resource.MaximumHours),
            (ContractFieldNames.FullTime, resource.FullTime)
        };

        foreach (var (key, value) in values)
        {
            if (value < decimal.Zero)
            {
                return NegativeMessage(key);
            }
        }

        if (resource.MinimumHours > resource.MaximumHours && resource.MaximumHours > decimal.Zero)
        {
            return $"Parameter '{ContractFieldNames.MinimumHours}' must not exceed '{ContractFieldNames.MaximumHours}'.";
        }

        if (resource.GuaranteedHours.HasValue
            && (resource.GuaranteedHours.Value < resource.MinimumHours || resource.GuaranteedHours.Value > resource.MaximumHours))
        {
            return $"Parameter '{ContractFieldNames.GuaranteedHours}' must be between " +
                   $"'{ContractFieldNames.MinimumHours}' and '{ContractFieldNames.MaximumHours}'.";
        }

        if (resource.ValidUntil.HasValue && resource.ValidUntil.Value <= resource.ValidFrom)
        {
            return $"Parameter '{ContractFieldNames.ValidUntil}' must be after '{ContractFieldNames.ValidFrom}'.";
        }

        return null;
    }

    public static string? FindHandlerRejection(ContractResource resource)
    {
        if (resource.MinimumHours > resource.MaximumHours)
        {
            return $"Parameter '{ContractFieldNames.MinimumHours}' must not exceed '{ContractFieldNames.MaximumHours}'; " +
                   $"a '{ContractFieldNames.MaximumHours}' of 0 means the band is not configured, so give both or neither.";
        }

        return null;
    }

    private static string NegativeMessage(string key) => $"Parameter '{key}' must not be negative.";
}
