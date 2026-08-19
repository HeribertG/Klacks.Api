// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a contract template (master data) from explicit, user-provided values — it never
/// derives hours from contract-type words. A contract's workload is defined through exactly one
/// of two mutually exclusive paths; ask the admin which one applies BEFORE calling this skill,
/// never guess:
/// (1) Fixed guaranteed hours — supply guaranteedHours (and optionally fullTime) for a contract
///     with an explicit hour value. minimumHours/maximumHours define a band around
///     guaranteedHours and default to it when omitted.
/// (2) Inherited workload — omit guaranteedHours entirely so the contract inherits the
///     company-wide monthly target hours (or the settings default); percent scales that
///     inherited value down and defaults to 100 when not supplied (a missing percent is normal,
///     not an error — do not insist on it). minimumHours/maximumHours/fullTime are not
///     meaningful in this path and are left unconfigured (0) unless explicitly supplied.
/// This skill does NOT assign the contract to an employee — use assign_contract_by_name or
/// assign_contract_to_client afterwards.
/// </summary>
/// <param name="name">Required. Contract template name.</param>
/// <param name="guaranteedHours">Optional. Guaranteed hours per payment interval for a fixed-hours contract (path 1). Omit entirely for a contract that inherits the company-wide workload (path 2) — never invent a value.</param>
/// <param name="validFrom">Required. Validity start date (YYYY-MM-DD).</param>
/// <param name="minimumHours">Optional, only meaningful together with guaranteedHours (path 1). Defaults to guaranteedHours when guaranteedHours is set; otherwise left unconfigured (0).</param>
/// <param name="maximumHours">Optional, only meaningful together with guaranteedHours (path 1). Defaults to guaranteedHours when guaranteedHours is set; otherwise left unconfigured (0).</param>
/// <param name="fullTime">Optional. Full-time reference hours for path 1; 0 means not configured. Not meaningful when guaranteedHours is omitted (path 2).</param>
/// <param name="nightRate">Optional. Night surcharge rate.</param>
/// <param name="holidayRate">Optional. Holiday surcharge rate.</param>
/// <param name="saRate">Optional. Saturday surcharge rate.</param>
/// <param name="soRate">Optional. Sunday surcharge rate.</param>
/// <param name="paymentInterval">Optional. Weekly, Biweekly, Monthly or Individual; defaults to Monthly.</param>
/// <param name="percent">Optional. Workload share in percent for path 2 (inherited workload); scales the company-wide monthly value and feeds absence macros. Defaults to 100 when omitted — a missing percent is normal, not an error.</param>
/// <param name="validUntil">Optional. Validity end date (YYYY-MM-DD); omit for open-ended.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_contract")]
public class CreateContractSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateContractSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("Missing required parameter 'name'.");
        }

        var guaranteedHours = GetParameter<decimal?>(parameters, "guaranteedHours");

        var validFrom = GetParameter<DateTime?>(parameters, "validFrom");
        if (!validFrom.HasValue)
        {
            return SkillResult.Error("Missing required parameter 'validFrom' (YYYY-MM-DD).");
        }

        var minimumHours = GetParameter<decimal?>(parameters, "minimumHours")
            ?? guaranteedHours
            ?? decimal.Zero;
        var maximumHours = GetParameter<decimal?>(parameters, "maximumHours")
            ?? guaranteedHours
            ?? decimal.Zero;
        var fullTime = GetParameter<decimal?>(parameters, "fullTime") ?? decimal.Zero;
        var nightRate = GetParameter<decimal?>(parameters, "nightRate") ?? decimal.Zero;
        var holidayRate = GetParameter<decimal?>(parameters, "holidayRate") ?? decimal.Zero;
        var saRate = GetParameter<decimal?>(parameters, "saRate") ?? decimal.Zero;
        var soRate = GetParameter<decimal?>(parameters, "soRate") ?? decimal.Zero;
        var percent = GetParameter<decimal?>(parameters, "percent");
        var validUntil = GetParameter<DateTime?>(parameters, "validUntil");

        var negativeNullable = new (string Key, decimal? Value)[]
        {
            ("guaranteedHours", guaranteedHours),
            ("percent", percent)
        };
        foreach (var (key, value) in negativeNullable)
        {
            if (value is < decimal.Zero)
            {
                return SkillResult.Error($"Parameter '{key}' must not be negative.");
            }
        }

        var negative = new (string Key, decimal Value)[]
        {
            ("minimumHours", minimumHours),
            ("maximumHours", maximumHours),
            ("fullTime", fullTime),
            ("nightRate", nightRate),
            ("holidayRate", holidayRate),
            ("saRate", saRate),
            ("soRate", soRate)
        };
        foreach (var (key, value) in negative)
        {
            if (value < decimal.Zero)
            {
                return SkillResult.Error($"Parameter '{key}' must not be negative.");
            }
        }

        if (minimumHours > maximumHours && maximumHours > decimal.Zero)
        {
            return SkillResult.Error("Parameter 'minimumHours' must not exceed 'maximumHours'.");
        }

        if (guaranteedHours.HasValue && (guaranteedHours.Value < minimumHours || guaranteedHours.Value > maximumHours))
        {
            return SkillResult.Error("Parameter 'guaranteedHours' must be between 'minimumHours' and 'maximumHours'.");
        }

        if (validUntil.HasValue && validUntil.Value <= validFrom.Value)
        {
            return SkillResult.Error("Parameter 'validUntil' must be after 'validFrom'.");
        }

        var paymentIntervalRaw = GetParameter<string>(parameters, "paymentInterval");
        var paymentInterval = PaymentInterval.Monthly;
        if (!string.IsNullOrWhiteSpace(paymentIntervalRaw)
            && (!Enum.TryParse(paymentIntervalRaw, ignoreCase: true, out paymentInterval) || !Enum.IsDefined(paymentInterval)))
        {
            return SkillResult.Error(
                $"Invalid paymentInterval '{paymentIntervalRaw}'. Use one of: {string.Join(", ", Enum.GetNames<PaymentInterval>())}.");
        }

        var resource = new ContractResource
        {
            Name = name.Trim(),
            GuaranteedHours = guaranteedHours,
            MinimumHours = minimumHours,
            MaximumHours = maximumHours,
            FullTime = fullTime,
            NightRate = nightRate,
            HolidayRate = holidayRate,
            WE1Rate = saRate,
            WE2Rate = soRate,
            PaymentInterval = paymentInterval,
            Percent = percent,
            ValidFrom = validFrom.Value,
            ValidUntil = validUntil
        };

        var created = await _mediator.Send(new PostCommand<ContractResource>(resource), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Contract '{resource.Name}' could not be created.");
        }

        string workloadNote;
        if (created.GuaranteedHours.HasValue)
        {
            var fullTimeNote = created.FullTime == decimal.Zero
                ? "; fullTime is not configured (0) — set it via update_contract when needed"
                : $", fullTime {created.FullTime}";
            workloadNote = $"guaranteed {created.GuaranteedHours}h, range {created.MinimumHours}-{created.MaximumHours}h{fullTimeNote}";
        }
        else
        {
            var percentNote = created.Percent.HasValue ? $"{created.Percent}%" : "100% (default, not supplied)";
            workloadNote = $"hours inherited from the company-wide value, scaled to {percentNote}";
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Name, created.GuaranteedHours, created.MinimumHours, created.MaximumHours, created.FullTime, created.Percent, PaymentInterval = created.PaymentInterval.ToString(), created.ValidFrom, created.ValidUntil },
            $"Contract '{created.Name}' created (id {created.Id}): {workloadNote}, interval {created.PaymentInterval}, valid from {created.ValidFrom:yyyy-MM-dd}. Use assign_contract_to_client to assign it to an employee.");
    }
}
