// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the pending planning-profile draft. When the base choice is an industry, every scheduling
/// rule tagged with that industry is copied as a NEW customer-owned row (fresh id, empty Industry and
/// empty import keys, so it is full-CRUD and never overwritten by a re-import — the templates themselves
/// are never mutated); when the base choice is scratch, a single blank customer-owned rule is created.
/// The collected field overrides are applied to every created rule, ACTIVE_INDUSTRIES is set to the
/// custom marker and the draft is cleared, all inside one transaction. RateRevisions on the templates
/// are not copied (v1 limitation). Domain problems (no draft, missing required parameters) surface as
/// <see cref="InvalidRequestException"/> so the calling skill can relay the real message.
/// </summary>
/// <param name="request">Identifies the pending draft by user and conversation key.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.PlanningProfile;
using Klacks.Api.Application.DTOs.PlanningProfile;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PlanningProfile;

public class ApplyPlanningProfileCommandHandler : IRequestHandler<ApplyPlanningProfileCommand, PlanningProfileApplyResult?>
{
    private readonly IPendingPlanningProfileDraftStore _draftStore;
    private readonly IPlanningProfileDraftValidator _validator;
    private readonly IPlanningProfileParameterCatalog _catalog;
    private readonly ISchedulingRuleRepository _schedulingRuleRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyPlanningProfileCommandHandler(
        IPendingPlanningProfileDraftStore draftStore,
        IPlanningProfileDraftValidator validator,
        IPlanningProfileParameterCatalog catalog,
        ISchedulingRuleRepository schedulingRuleRepository,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork)
    {
        _draftStore = draftStore;
        _validator = validator;
        _catalog = catalog;
        _schedulingRuleRepository = schedulingRuleRepository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlanningProfileApplyResult?> Handle(ApplyPlanningProfileCommand request, CancellationToken cancellationToken)
    {
        var draft = _draftStore.Get(request.UserId, request.ConversationKey);
        if (draft is null)
        {
            throw new InvalidRequestException(
                "No planning profile draft in progress. Start one with start_planning_profile_setup before applying.");
        }

        var missing = _validator.GetMissingRequired(draft);
        if (missing.Count > 0)
        {
            throw new InvalidRequestException(
                $"Cannot apply the planning profile: still missing required parameter(s): {string.Join(", ", missing)}.");
        }

        var baseChoice = draft.Parameters[PlanningProfileParameterNames.BaseIndustry].Trim().ToLowerInvariant();
        var overrides = CollectOverrides(draft);

        var createdNames = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var names = string.Equals(baseChoice, PlanningProfileBaseChoices.Scratch, StringComparison.OrdinalIgnoreCase)
                ? await CreateScratchRuleAsync(draft, overrides)
                : await CopyIndustryRulesAsync(baseChoice, overrides);

            await _settingsRepository.UpsertSettingAsync(SettingKeys.ActiveIndustries, IndustrySlugs.Custom);
            await _unitOfWork.CompleteAsync();
            return names;
        });

        _draftStore.Clear(request.UserId, request.ConversationKey);

        return new PlanningProfileApplyResult
        {
            BaseChoice = baseChoice,
            CreatedRuleCount = createdNames.Count,
            CreatedRuleNames = createdNames,
            AppliedOverrides = overrides,
            ActiveIndustries = IndustrySlugs.Custom
        };
    }

    private async Task<List<string>> CreateScratchRuleAsync(PlanningProfileDraft draft, IReadOnlyDictionary<string, string> overrides)
    {
        var name = draft.Parameters.TryGetValue(PlanningProfileParameterNames.ProfileName, out var explicitName)
                   && !string.IsNullOrWhiteSpace(explicitName)
            ? explicitName.Trim()
            : PlanningProfileDraftDefaults.DefaultScratchRuleName;

        var rule = new SchedulingRule
        {
            Id = Guid.NewGuid(),
            Name = name,
            Industry = string.Empty,
            ImportSourceKey = string.Empty,
            ImportContentHash = string.Empty
        };

        ApplyOverrides(rule, overrides);
        await _schedulingRuleRepository.Add(rule);
        return new List<string> { rule.Name };
    }

    private async Task<List<string>> CopyIndustryRulesAsync(string industry, IReadOnlyDictionary<string, string> overrides)
    {
        var templates = await _schedulingRuleRepository.GetByIndustryAsync(industry);
        var createdNames = new List<string>();

        foreach (var template in templates)
        {
            var copy = CopyAsCustomerOwned(template);
            ApplyOverrides(copy, overrides);
            await _schedulingRuleRepository.Add(copy);
            createdNames.Add(copy.Name);
        }

        return createdNames;
    }

    private static SchedulingRule CopyAsCustomerOwned(SchedulingRule template) => new()
    {
        Id = Guid.NewGuid(),
        Name = template.Name,
        Industry = string.Empty,
        ImportSourceKey = string.Empty,
        ImportContentHash = string.Empty,
        MaxWorkDays = template.MaxWorkDays,
        MinRestDays = template.MinRestDays,
        MinPauseHours = template.MinPauseHours,
        MaxOptimalGap = template.MaxOptimalGap,
        MaxDailyHours = template.MaxDailyHours,
        MaxWeeklyHours = template.MaxWeeklyHours,
        MaxConsecutiveDays = template.MaxConsecutiveDays,
        DefaultWorkingHours = template.DefaultWorkingHours,
        OvertimeThreshold = template.OvertimeThreshold,
        GuaranteedHours = template.GuaranteedHours,
        MaximumHours = template.MaximumHours,
        MinimumHours = template.MinimumHours,
        FullTimeHours = template.FullTimeHours,
        VacationDaysPerYear = template.VacationDaysPerYear,
        NightRate = template.NightRate,
        HolidayRate = template.HolidayRate,
        WE1Rate = template.WE1Rate,
        WE2Rate = template.WE2Rate,
        WE3Rate = template.WE3Rate,
        NightStart = template.NightStart,
        NightEnd = template.NightEnd,
        WorkOnMonday = template.WorkOnMonday,
        WorkOnTuesday = template.WorkOnTuesday,
        WorkOnWednesday = template.WorkOnWednesday,
        WorkOnThursday = template.WorkOnThursday,
        WorkOnFriday = template.WorkOnFriday,
        WorkOnSaturday = template.WorkOnSaturday,
        WorkOnSunday = template.WorkOnSunday,
        PerformsShiftWork = template.PerformsShiftWork,
        OvertimeBasis = template.OvertimeBasis,
        OvertimeRateMode = template.OvertimeRateMode,
        OvertimeTier1AfterHours = template.OvertimeTier1AfterHours,
        OvertimeTier1Rate = template.OvertimeTier1Rate,
        OvertimeTier2AfterHours = template.OvertimeTier2AfterHours,
        OvertimeTier2Rate = template.OvertimeTier2Rate,
        OvertimeTier3AfterHours = template.OvertimeTier3AfterHours,
        OvertimeTier3Rate = template.OvertimeTier3Rate
    };

    private Dictionary<string, string> CollectOverrides(PlanningProfileDraft draft)
    {
        var overrides = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var definition in _catalog.GetParameters())
        {
            if (!IsOverrideParameter(definition.Name))
            {
                continue;
            }

            if (draft.Parameters.TryGetValue(definition.Name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                overrides[definition.Name] = value.Trim();
            }
        }

        return overrides;
    }

    private static bool IsOverrideParameter(string name)
    {
        return name != PlanningProfileParameterNames.BaseIndustry
            && name != PlanningProfileParameterNames.ProfileName;
    }

    private static void ApplyOverrides(SchedulingRule rule, IReadOnlyDictionary<string, string> overrides)
    {
        foreach (var (name, value) in overrides)
        {
            ApplyOverride(rule, name, value);
        }
    }

    private static void ApplyOverride(SchedulingRule rule, string name, string value)
    {
        switch (name)
        {
            case PlanningProfileParameterNames.DefaultWorkingHours:
                rule.DefaultWorkingHours = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.FullTimeHours:
                rule.FullTimeHours = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.MaxDailyHours:
                rule.MaxDailyHours = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.MaxWeeklyHours:
                rule.MaxWeeklyHours = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.MaxConsecutiveDays:
                rule.MaxConsecutiveDays = ParseInt(value);
                break;
            case PlanningProfileParameterNames.MinRestDays:
                rule.MinRestDays = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.MinPauseHours:
                rule.MinPauseHours = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.OvertimeThreshold:
                rule.OvertimeThreshold = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.OvertimeBasis:
                rule.OvertimeBasis = Enum.Parse<OvertimeBasis>(value, ignoreCase: true);
                break;
            case PlanningProfileParameterNames.NightStart:
                rule.NightStart = value;
                break;
            case PlanningProfileParameterNames.NightEnd:
                rule.NightEnd = value;
                break;
            case PlanningProfileParameterNames.NightRate:
                rule.NightRate = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.HolidayRate:
                rule.HolidayRate = ParseDecimal(value);
                break;
            case PlanningProfileParameterNames.VacationDaysPerYear:
                rule.VacationDaysPerYear = ParseInt(value);
                break;
            default:
                throw new InvalidOperationException(
                    $"Planning-profile override parameter '{name}' has no field mapping in ApplyOverride. " +
                    "A catalog override parameter was added without a matching case; add it here so the value " +
                    "is not silently dropped.");
        }
    }

    private static decimal ParseDecimal(string value) => decimal.Parse(value, NumberStyles.Number, CultureInfo.InvariantCulture);

    private static int ParseInt(string value) => int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
}
