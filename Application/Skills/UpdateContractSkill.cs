// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a contract template (master data). Only fields supplied as parameters are changed;
/// hour values must not be negative and minimumHours must not exceed maximumHours. This skill
/// does NOT assign a contract to an employee — use assign_contract_by_name or
/// assign_contract_to_client for that.
/// </summary>
/// <param name="contractId">Required. UUID of the contract to update.</param>
/// <param name="name">Optional. New contract name.</param>
/// <param name="guaranteedHours">Optional. New guaranteed hours.</param>
/// <param name="minimumHours">Optional. New minimum hours.</param>
/// <param name="maximumHours">Optional. New maximum hours.</param>
/// <param name="fullTime">Optional. New full-time reference hours.</param>
/// <param name="nightRate">Optional. New night surcharge rate.</param>
/// <param name="holidayRate">Optional. New holiday surcharge rate.</param>
/// <param name="saRate">Optional. New Saturday surcharge rate.</param>
/// <param name="soRate">Optional. New Sunday surcharge rate.</param>
/// <param name="validFrom">Optional. New validity start date (YYYY-MM-DD).</param>
/// <param name="validUntil">Optional. New validity end date (YYYY-MM-DD).</param>
/// <param name="clearValidUntil">Optional. If true, removes the validity end date.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_contract")]
public class UpdateContractSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateContractSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var contractId = GetRequiredGuid(parameters, "contractId");

        ContractResource contract;
        try
        {
            contract = await _mediator.Send(new GetQuery<ContractResource>(contractId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Contract '{contractId}' not found.");
        }

        var changed = new List<string>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != contract.Name)
        {
            contract.Name = name.Trim();
            changed.Add("name");
        }

        var decimalFields = new (string Key, Func<decimal> Get, Action<decimal> Set)[]
        {
            ("guaranteedHours", () => contract.GuaranteedHours, v => contract.GuaranteedHours = v),
            ("minimumHours", () => contract.MinimumHours, v => contract.MinimumHours = v),
            ("maximumHours", () => contract.MaximumHours, v => contract.MaximumHours = v),
            ("fullTime", () => contract.FullTime, v => contract.FullTime = v),
            ("nightRate", () => contract.NightRate, v => contract.NightRate = v),
            ("holidayRate", () => contract.HolidayRate, v => contract.HolidayRate = v),
            ("saRate", () => contract.SaRate, v => contract.SaRate = v),
            ("soRate", () => contract.SoRate, v => contract.SoRate = v),
        };

        foreach (var field in decimalFields)
        {
            var value = GetParameter<decimal?>(parameters, field.Key);
            if (!value.HasValue)
            {
                continue;
            }

            if (value.Value < decimal.Zero)
            {
                return SkillResult.Error($"Parameter '{field.Key}' must not be negative.");
            }

            if (value.Value == field.Get())
            {
                continue;
            }

            field.Set(value.Value);
            changed.Add(field.Key);
        }

        var validFrom = GetParameter<DateTime?>(parameters, "validFrom");
        if (validFrom.HasValue && validFrom.Value != contract.ValidFrom)
        {
            contract.ValidFrom = validFrom.Value;
            changed.Add("validFrom");
        }

        var clearValidUntil = GetParameter<bool>(parameters, "clearValidUntil", false);
        if (clearValidUntil)
        {
            if (contract.ValidUntil != null)
            {
                contract.ValidUntil = null;
                changed.Add("validUntil");
            }
        }
        else
        {
            var validUntil = GetParameter<DateTime?>(parameters, "validUntil");
            if (validUntil.HasValue && validUntil.Value != contract.ValidUntil)
            {
                contract.ValidUntil = validUntil.Value;
                changed.Add("validUntil");
            }
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ContractId = contractId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — contract left unchanged.");
        }

        if (contract.MinimumHours > contract.MaximumHours && contract.MaximumHours > decimal.Zero)
        {
            return SkillResult.Error("minimumHours must not exceed maximumHours.");
        }

        if (contract.ValidUntil.HasValue && contract.ValidUntil.Value < contract.ValidFrom)
        {
            return SkillResult.Error("validUntil must not be before validFrom.");
        }

        var updated = await _mediator.Send(new PutCommand<ContractResource>(contract), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating contract '{contractId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                ContractId = contractId,
                ChangedFields = changed,
                updated.Name,
                updated.GuaranteedHours,
                updated.MinimumHours,
                updated.MaximumHours,
                updated.ValidFrom,
                updated.ValidUntil
            },
            $"Contract '{updated.Name}' updated ({string.Join(", ", changed)}).");
    }
}
