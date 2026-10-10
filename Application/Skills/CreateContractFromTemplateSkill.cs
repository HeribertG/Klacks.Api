// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a new contract by copying an existing contract template and adapting only the values the
/// administrator chose. Every other value (the night, holiday, Saturday, Sunday and WE3 time credits, the night
/// window, the scheduling rule; the individual pay period only while the payment interval is unchanged) is copied unchanged; the template's validUntil is
/// not copied, so the new contract is open-ended. A contract's workload is defined through exactly one of two
/// mutually exclusive paths, and giving both guaranteedHours and percent is rejected: guaranteedHours switches
/// to fixed hours (percent is cleared, minimumHours and maximumHours default to guaranteedHours), percent
/// switches to the inherited company-wide workload (guaranteedHours is cleared, minimumHours and maximumHours
/// fall back to 0). It never derives hours from contract-type words and does NOT assign the contract to an
/// employee.
/// </summary>
/// <param name="templateContractId">Required. Contract id (UUID) or exact contract name of the existing contract to copy.</param>
/// <param name="name">Required. Name of the new contract; must not equal the name of any existing contract.</param>
/// <param name="validFrom">Required. Validity start date of the new contract (YYYY-MM-DD).</param>
/// <param name="guaranteedHours">Optional. Fixed guaranteed hours per interval (workload path 1); excludes percent.</param>
/// <param name="percent">Optional. Workload percent of the company-wide value (workload path 2); excludes guaranteedHours.</param>
/// <param name="minimumHours">Optional. Minimum hours of the band; overrides the value derived from the workload path.</param>
/// <param name="maximumHours">Optional. Maximum hours of the band; overrides the value derived from the workload path.</param>
/// <param name="fullTime">Optional. Full-time reference hours.</param>
/// <param name="paymentInterval">Optional. Weekly, Biweekly, Monthly, Individual or MonthlyTargetHours.</param>
/// <param name="performsShiftWork">Optional. Whether the employee works late or night shifts.</param>
/// <param name="workdays">Optional. Working weekdays as a comma separated list of Mon,Tue,Wed,Thu,Fri,Sat,Sun; unlisted days are set to not worked.</param>
/// <param name="region">Optional. State, canton or region code that selects the holiday calendar; must match exactly one calendar.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services.Contracts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_contract_from_template")]
public class CreateContractFromTemplateSkill : BaseSkillImplementation
{
    private const string TemplateCalendarOwner = "template's";

    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;
    private readonly IContractRepository _contractRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ICountryResolver _countryResolver;

    public CreateContractFromTemplateSkill(
        IMediator mediator,
        ICompanyClock companyClock,
        IContractRepository contractRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        ICountryResolver countryResolver)
    {
        _mediator = mediator;
        _companyClock = companyClock;
        _contractRepository = contractRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _countryResolver = countryResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (resolvedTemplateId, templateError) = await ContractReferenceResolver.ResolveIdAsync(
            _contractRepository, GetParameter<string>(parameters, ContractFieldNames.TemplateContractId),
            ContractFieldNames.TemplateContractId, ContractNameMatchMode.ExactOrUniquePartial, cancellationToken);
        if (resolvedTemplateId == null)
        {
            return SkillResult.Error(templateError!);
        }

        var templateId = resolvedTemplateId.Value;

        var name = GetParameter<string>(parameters, ContractFieldNames.Name)?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return SkillResult.Error($"Missing required parameter '{ContractFieldNames.Name}'.");
        }

        var validFromRaw = GetParameter<string>(parameters, ContractFieldNames.ValidFrom);
        if (string.IsNullOrWhiteSpace(validFromRaw))
        {
            return SkillResult.Error($"Missing required parameter '{ContractFieldNames.ValidFrom}' (YYYY-MM-DD).");
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (validFrom, invalidValidFrom) = SkillDateParser.ParseOptionalUtcDate(validFromRaw, today, context.UserLanguage);
        if (invalidValidFrom || !validFrom.HasValue)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor(ContractFieldNames.ValidFrom, validFromRaw));
        }

        if (!ContractTemplateOverrides.TryRead(parameters, out var overrides, out var overridesError))
        {
            return SkillResult.Error(overridesError!);
        }

        var guaranteedHours = overrides.GuaranteedHours;
        var percent = overrides.Percent;
        if (guaranteedHours.HasValue && percent.HasValue)
        {
            return SkillResult.Error(
                $"Parameters '{ContractFieldNames.GuaranteedHours}' and '{ContractFieldNames.Percent}' are mutually " +
                "exclusive: fixed guaranteed hours or a workload percent of the company-wide value. Ask the " +
                "administrator which one applies.");
        }

        PaymentInterval? paymentInterval = null;
        var paymentIntervalRaw = GetParameter<string>(parameters, ContractFieldNames.PaymentInterval);
        if (!string.IsNullOrWhiteSpace(paymentIntervalRaw))
        {
            if (!ContractPaymentIntervalParser.TryParse(paymentIntervalRaw, out var parsedInterval, out var intervalError))
            {
                return SkillResult.Error(intervalError!);
            }

            paymentInterval = parsedInterval;
        }

        IReadOnlySet<DayOfWeek>? workdays = null;
        var workdaysRaw = GetParameter<string>(parameters, ContractFieldNames.Workdays);
        if (!string.IsNullOrWhiteSpace(workdaysRaw))
        {
            if (!ContractWorkdays.TryParse(workdaysRaw, out var parsedDays, out var workdaysError))
            {
                return SkillResult.Error(workdaysError!);
            }

            workdays = parsedDays;
        }

        ContractResource template;
        try
        {
            template = await _mediator.Send(new GetQuery<ContractResource>(templateId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Template contract '{templateId}' not found.");
        }

        var existingContracts = await _contractRepository.List();
        if (existingContracts.Any(contract => string.Equals(contract.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase)))
        {
            return SkillResult.Error(
                $"A contract named '{name}' already exists. Ask the administrator for a different name — " +
                "no suffix is added automatically.");
        }

        var resource = ContractTemplateCopier.CopyAsNew(template);
        resource.Name = name;
        resource.ValidFrom = validFrom.Value;

        ApplyWorkload(resource, overrides);

        if (overrides.FullTime.HasValue)
        {
            resource.FullTime = overrides.FullTime.Value;
        }

        if (overrides.PerformsShiftWork.HasValue)
        {
            resource.PerformsShiftWork = overrides.PerformsShiftWork;
        }

        if (workdays != null)
        {
            ContractWorkdays.Apply(resource, workdays);
        }

        var intervalViolation = ApplyPaymentInterval(resource, paymentInterval);
        if (intervalViolation != null)
        {
            return SkillResult.Error(intervalViolation);
        }

        var regionRaw = GetParameter<string>(parameters, ContractFieldNames.Region);
        if (!string.IsNullOrWhiteSpace(regionRaw))
        {
            var regionError = await ApplyRegionAsync(resource, regionRaw.Trim(), cancellationToken);
            if (regionError != null)
            {
                return SkillResult.Error(regionError);
            }
        }

        var violation = ContractResourceValidator.FindViolation(resource)
            ?? ContractResourceValidator.FindHandlerRejection(resource);
        if (violation != null)
        {
            return SkillResult.Error($"The adapted contract would be invalid: {violation}");
        }

        var created = await _mediator.Send(new PostCommand<ContractResource>(resource), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error($"Contract '{name}' could not be created.");
        }

        return BuildResult(template, created, guaranteedHours.HasValue, today);
    }

    private static void ApplyWorkload(ContractResource resource, ContractTemplateOverrides overrides)
    {
        if (overrides.GuaranteedHours.HasValue)
        {
            resource.GuaranteedHours = overrides.GuaranteedHours;
            resource.Percent = null;
            resource.MinimumHours = overrides.MinimumHours ?? overrides.GuaranteedHours.Value;
            resource.MaximumHours = overrides.MaximumHours ?? overrides.GuaranteedHours.Value;
            return;
        }

        if (overrides.Percent.HasValue)
        {
            resource.GuaranteedHours = null;
            resource.Percent = overrides.Percent;
            resource.MinimumHours = overrides.MinimumHours ?? decimal.Zero;
            resource.MaximumHours = overrides.MaximumHours ?? decimal.Zero;
            return;
        }

        if (overrides.MinimumHours.HasValue)
        {
            resource.MinimumHours = overrides.MinimumHours.Value;
        }

        if (overrides.MaximumHours.HasValue)
        {
            resource.MaximumHours = overrides.MaximumHours.Value;
        }
    }

    private static string? ApplyPaymentInterval(ContractResource resource, PaymentInterval? paymentInterval)
    {
        if (!paymentInterval.HasValue)
        {
            return null;
        }

        resource.PaymentInterval = paymentInterval.Value;

        if (paymentInterval.Value != PaymentInterval.Individual)
        {
            resource.IndividualPeriodId = null;
            return null;
        }

        return resource.IndividualPeriodId == null
            ? $"The template has no individual pay period, so '{ContractFieldNames.PaymentInterval}' cannot be switched " +
              $"to {nameof(PaymentInterval.Individual)}. Choose another interval or a template that already has one."
            : null;
    }

    private async Task<string?> ApplyRegionAsync(
        ContractResource resource, string region, CancellationToken cancellationToken)
    {
        var (calendarId, error) = await ContractRegionCalendarResolver.ChooseAsync(
            _countryResolver, _calendarSelectionRepository, region, resource.CalendarSelectionId,
            TemplateCalendarOwner, cancellationToken);

        if (error != null)
        {
            return error;
        }

        resource.CalendarSelectionId = calendarId;
        return null;
    }

    private static SkillResult BuildResult(
        ContractResource template, ContractResource created, bool guaranteedHoursGiven, DateTime today)
    {
        var adapted = ContractTemplateCopier.AdaptedFields(template, created);
        var copied = ContractTemplateCopier.CopiedFields(adapted);

        var warnings = new List<string>();
        if (guaranteedHoursGiven && created.PaymentInterval == PaymentInterval.MonthlyTargetHours)
        {
            warnings.Add(
                $"With the {nameof(PaymentInterval.MonthlyTargetHours)} interval the company-wide monthly table decides " +
                $"the hours of every month that has a row there, so '{ContractFieldNames.GuaranteedHours}' only applies " +
                $"in months without a row. Ask the administrator whether a '{ContractFieldNames.Percent}' or another " +
                "interval is meant.");
        }

        if (template.ValidUntil.HasValue && template.ValidUntil.Value < today)
        {
            warnings.Add(
                $"The template expired on {template.ValidUntil.Value:yyyy-MM-dd}; the new contract is a fresh copy " +
                "without an end date. Check that its values are still current.");
        }

        var ruleOverridden = adapted.Where(field => ContractTemplateCopier.RuleOverridableFields.Contains(field)).ToList();
        if (created.SchedulingRuleId.HasValue && created.GuaranteedHours is null)
        {
            ruleOverridden.Remove(ContractFieldNames.GuaranteedHours);
        }

        if (created.SchedulingRuleId.HasValue && ruleOverridden.Count > 0)
        {
            warnings.Add(
                $"The copied scheduling rule may override these adapted values: {string.Join(", ", ruleOverridden)}. " +
                "A value the rule sets wins over the contract's own value, so check the rule in the contract settings page.");
        }

        var data = new
        {
            created.Id,
            created.Name,
            TemplateContractId = template.Id,
            TemplateName = template.Name,
            created.GuaranteedHours,
            created.Percent,
            created.MinimumHours,
            created.MaximumHours,
            created.FullTime,
            PaymentInterval = created.PaymentInterval.ToString(),
            created.PerformsShiftWork,
            Workdays = ContractWorkdays.Format(ContractWorkdays.Of(created)),
            created.CalendarSelectionId,
            created.ValidFrom,
            created.ValidUntil,
            AdaptedFields = adapted,
            CopiedFields = copied,
            Warnings = warnings
        };

        var message =
            $"Contract '{created.Name}' created (id {created.Id}) as a copy of template '{template.Name}'. " +
            $"Adapted: {string.Join(", ", adapted)}. Copied unchanged: {string.Join(", ", copied)}. " +
            "Surcharges are time credits, never money; adjust them or the end date afterwards with update_contract. " +
            "Use assign_contract_by_name to assign the contract to an employee." +
            (warnings.Count > 0 ? " Warning: " + string.Join(" ", warnings) : string.Empty);

        return SkillResult.SuccessResult(data, message);
    }
}
