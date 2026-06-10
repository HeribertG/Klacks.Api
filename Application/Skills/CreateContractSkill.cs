// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a contract template (master data) from explicit, user-provided values — it never
/// derives hours from contract-type words. Minimum and maximum hours default to the guaranteed
/// hours (fixed-hours contract) when omitted. This skill does NOT assign the contract to an
/// employee — use assign_contract_by_name or assign_contract_to_client afterwards.
/// </summary>
/// <param name="name">Required. Contract template name.</param>
/// <param name="guaranteedHours">Required. Guaranteed hours per payment interval.</param>
/// <param name="validFrom">Required. Validity start date (YYYY-MM-DD).</param>
/// <param name="minimumHours">Optional. Defaults to guaranteedHours.</param>
/// <param name="maximumHours">Optional. Defaults to guaranteedHours.</param>
/// <param name="fullTime">Optional. Full-time reference hours; 0 means not configured.</param>
/// <param name="nightRate">Optional. Night surcharge rate.</param>
/// <param name="holidayRate">Optional. Holiday surcharge rate.</param>
/// <param name="saRate">Optional. Saturday surcharge rate.</param>
/// <param name="soRate">Optional. Sunday surcharge rate.</param>
/// <param name="paymentInterval">Optional. Weekly, Biweekly, Monthly or Individual; defaults to Monthly.</param>
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
        if (!guaranteedHours.HasValue)
        {
            return SkillResult.Error("Missing required parameter 'guaranteedHours'. Ask the user for the real value — never derive hours from contract-type words.");
        }

        var validFrom = GetParameter<DateTime?>(parameters, "validFrom");
        if (!validFrom.HasValue)
        {
            return SkillResult.Error("Missing required parameter 'validFrom' (YYYY-MM-DD).");
        }

        var minimumHours = GetParameter<decimal?>(parameters, "minimumHours") ?? guaranteedHours.Value;
        var maximumHours = GetParameter<decimal?>(parameters, "maximumHours") ?? guaranteedHours.Value;
        var fullTime = GetParameter<decimal?>(parameters, "fullTime") ?? decimal.Zero;
        var nightRate = GetParameter<decimal?>(parameters, "nightRate") ?? decimal.Zero;
        var holidayRate = GetParameter<decimal?>(parameters, "holidayRate") ?? decimal.Zero;
        var saRate = GetParameter<decimal?>(parameters, "saRate") ?? decimal.Zero;
        var soRate = GetParameter<decimal?>(parameters, "soRate") ?? decimal.Zero;
        var validUntil = GetParameter<DateTime?>(parameters, "validUntil");

        var negative = new (string Key, decimal Value)[]
        {
            ("guaranteedHours", guaranteedHours.Value),
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

        if (minimumHours > maximumHours)
        {
            return SkillResult.Error("Parameter 'minimumHours' must not exceed 'maximumHours'.");
        }

        if (guaranteedHours.Value < minimumHours || guaranteedHours.Value > maximumHours)
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
            GuaranteedHours = guaranteedHours.Value,
            MinimumHours = minimumHours,
            MaximumHours = maximumHours,
            FullTime = fullTime,
            NightRate = nightRate,
            HolidayRate = holidayRate,
            SaRate = saRate,
            SoRate = soRate,
            PaymentInterval = paymentInterval,
            ValidFrom = validFrom.Value,
            ValidUntil = validUntil
        };

        var created = await _mediator.Send(new PostCommand<ContractResource>(resource), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Contract '{resource.Name}' could not be created.");
        }

        var fullTimeNote = created.FullTime == decimal.Zero
            ? "; fullTime is not configured (0) — set it via update_contract when needed"
            : $", fullTime {created.FullTime}";

        return SkillResult.SuccessResult(
            new { created.Id, created.Name, created.GuaranteedHours, created.MinimumHours, created.MaximumHours, created.FullTime, PaymentInterval = created.PaymentInterval.ToString(), created.ValidFrom, created.ValidUntil },
            $"Contract '{created.Name}' created (id {created.Id}): guaranteed {created.GuaranteedHours}h, range {created.MinimumHours}-{created.MaximumHours}h{fullTimeNote}, interval {created.PaymentInterval}, valid from {created.ValidFrom:yyyy-MM-dd}. Use assign_contract_to_client to assign it to an employee.");
    }
}
