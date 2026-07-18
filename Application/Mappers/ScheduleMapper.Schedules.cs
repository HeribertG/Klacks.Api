// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partial class for Schedule, WorkChange, Expenses, Notes, SchedulingRule and CounterRule mappings.
/// </summary>
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Scheduling;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

public partial class ScheduleMapper
{
    [MapProperty(nameof(ShiftDayAssignment.RequiredQualifications), nameof(ShiftScheduleResource.Qualifications))]
    public partial ShiftScheduleResource ToShiftScheduleResource(ShiftDayAssignment assignment);

    public partial List<ShiftScheduleResource> ToShiftScheduleResourceList(List<ShiftDayAssignment> assignments);

    private ScheduleQualificationResource ToScheduleQualificationResource(ShiftRequiredQualification required) => new()
    {
        QualificationId = required.QualificationId,
        Emoji = required.Qualification?.Emoji,
        Name = required.Qualification?.Name ?? new MultiLanguage(),
        Level = required.MinLevel,
        ValidUntil = null,
    };

    [MapProperty(nameof(ScheduleCell.Description), nameof(WorkScheduleResource.Description), Use = nameof(MapDescriptionJsonToMultiLanguage))]
    public partial WorkScheduleResource ToWorkScheduleResource(ScheduleCell entry);

    public partial List<WorkScheduleResource> ToWorkScheduleResourceList(List<ScheduleCell> entries);

    private MultiLanguage? MapDescriptionJsonToMultiLanguage(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<MultiLanguage>(json);
        }
        catch
        {
            var ml = new MultiLanguage();
            foreach (var lang in MultiLanguage.CoreLanguages)
                ml.SetValue(lang, json);
            return ml;
        }
    }

    public partial WorkScheduleClientResource ToWorkScheduleClientResource(Client client);

    public partial List<WorkScheduleClientResource> ToWorkScheduleClientResourceList(List<Client> clients);

    private ScheduleQualificationResource ToScheduleQualificationResource(ClientQualification qualification) => new()
    {
        QualificationId = qualification.QualificationId,
        Emoji = qualification.Qualification?.Emoji,
        Name = qualification.Qualification?.Name ?? new MultiLanguage(),
        Level = qualification.Level,
        ValidUntil = qualification.ValidUntil,
    };

    public partial WorkChangeResource ToWorkChangeResource(WorkChange workChange);

    public partial List<WorkChangeResource> ToWorkChangeResourceList(List<WorkChange> workChanges);

    [MapperIgnoreTarget(nameof(WorkChange.CreateTime))]
    [MapperIgnoreTarget(nameof(WorkChange.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(WorkChange.UpdateTime))]
    [MapperIgnoreTarget(nameof(WorkChange.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(WorkChange.DeletedTime))]
    [MapperIgnoreTarget(nameof(WorkChange.IsDeleted))]
    [MapperIgnoreTarget(nameof(WorkChange.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(WorkChange.Work))]
    [MapperIgnoreTarget(nameof(WorkChange.ReplaceClient))]
    public partial WorkChange ToWorkChangeEntity(WorkChangeResource resource);

    public partial ExpensesResource ToExpensesResource(Expenses expenses);

    public partial List<ExpensesResource> ToExpensesResourceList(List<Expenses> expenses);

    [MapperIgnoreTarget(nameof(Expenses.CreateTime))]
    [MapperIgnoreTarget(nameof(Expenses.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Expenses.UpdateTime))]
    [MapperIgnoreTarget(nameof(Expenses.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Expenses.DeletedTime))]
    [MapperIgnoreTarget(nameof(Expenses.IsDeleted))]
    [MapperIgnoreTarget(nameof(Expenses.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Expenses.Work))]
    public partial Expenses ToExpensesEntity(ExpensesResource resource);

    public partial ScheduleNoteResource ToScheduleNoteResource(ScheduleNote scheduleNote);
    public partial List<ScheduleNoteResource> ToScheduleNoteResourceList(List<ScheduleNote> scheduleNotes);

    [MapperIgnoreTarget(nameof(ScheduleNote.CreateTime))]
    [MapperIgnoreTarget(nameof(ScheduleNote.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ScheduleNote.UpdateTime))]
    [MapperIgnoreTarget(nameof(ScheduleNote.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ScheduleNote.DeletedTime))]
    [MapperIgnoreTarget(nameof(ScheduleNote.IsDeleted))]
    [MapperIgnoreTarget(nameof(ScheduleNote.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ScheduleNote.Client))]
    public partial ScheduleNote ToScheduleNoteEntity(ScheduleNoteResource resource);

    public partial ScheduleCommandResource ToScheduleCommandResource(ScheduleCommand scheduleCommand);
    public partial List<ScheduleCommandResource> ToScheduleCommandResourceList(List<ScheduleCommand> scheduleCommands);

    [MapperIgnoreTarget(nameof(ScheduleCommand.CreateTime))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.UpdateTime))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.DeletedTime))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.IsDeleted))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(ScheduleCommand.Client))]
    public partial ScheduleCommand ToScheduleCommandEntity(ScheduleCommandResource resource);

    public partial SchedulingRuleResource ToSchedulingRuleResource(SchedulingRule rule);

    [MapperIgnoreTarget(nameof(SchedulingRule.CreateTime))]
    [MapperIgnoreTarget(nameof(SchedulingRule.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(SchedulingRule.UpdateTime))]
    [MapperIgnoreTarget(nameof(SchedulingRule.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(SchedulingRule.DeletedTime))]
    [MapperIgnoreTarget(nameof(SchedulingRule.IsDeleted))]
    [MapperIgnoreTarget(nameof(SchedulingRule.CurrentUserDeleted))]
    public partial SchedulingRule ToSchedulingRuleEntity(SchedulingRuleResource resource);

    public void UpdateSchedulingRuleEntity(SchedulingRule target, SchedulingRuleResource source)
    {
        target.Name = source.Name;
        target.MaxWorkDays = source.MaxWorkDays;
        target.MinRestDays = source.MinRestDays;
        target.MinPauseHours = source.MinPauseHours;
        target.MaxOptimalGap = source.MaxOptimalGap;
        target.MaxDailyHours = source.MaxDailyHours;
        target.MaxWeeklyHours = source.MaxWeeklyHours;
        target.MaxConsecutiveDays = source.MaxConsecutiveDays;
        target.DefaultWorkingHours = source.DefaultWorkingHours;
        target.OvertimeThreshold = source.OvertimeThreshold;
        target.GuaranteedHours = source.GuaranteedHours;
        target.MaximumHours = source.MaximumHours;
        target.MinimumHours = source.MinimumHours;
        target.FullTimeHours = source.FullTimeHours;
        target.VacationDaysPerYear = source.VacationDaysPerYear;
        target.NightRate = source.NightRate;
        target.HolidayRate = source.HolidayRate;
        target.WE1Rate = source.WE1Rate;
        target.WE2Rate = source.WE2Rate;
        target.WE3Rate = source.WE3Rate;
        target.NightStart = source.NightStart;
        target.NightEnd = source.NightEnd;
        target.WorkOnMonday = source.WorkOnMonday;
        target.WorkOnTuesday = source.WorkOnTuesday;
        target.WorkOnWednesday = source.WorkOnWednesday;
        target.WorkOnThursday = source.WorkOnThursday;
        target.WorkOnFriday = source.WorkOnFriday;
        target.WorkOnSaturday = source.WorkOnSaturday;
        target.WorkOnSunday = source.WorkOnSunday;
        target.PerformsShiftWork = source.PerformsShiftWork;
    }

    public partial CounterRuleResource ToCounterRuleResource(CounterRule rule);

    [MapperIgnoreTarget(nameof(CounterRule.CreateTime))]
    [MapperIgnoreTarget(nameof(CounterRule.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(CounterRule.UpdateTime))]
    [MapperIgnoreTarget(nameof(CounterRule.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(CounterRule.DeletedTime))]
    [MapperIgnoreTarget(nameof(CounterRule.IsDeleted))]
    [MapperIgnoreTarget(nameof(CounterRule.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(CounterRule.ImportSourceKey))]
    [MapperIgnoreTarget(nameof(CounterRule.ImportContentHash))]
    public partial CounterRule ToCounterRuleEntity(CounterRuleResource resource);

    public void UpdateCounterRuleEntity(CounterRule target, CounterRuleResource source)
    {
        target.EventType = source.EventType;
        target.Period = source.Period;
        target.Threshold = source.Threshold;
        target.HoursThreshold = source.HoursThreshold;
        target.Enforcement = source.Enforcement;
        target.SchedulingRuleId = source.SchedulingRuleId;
    }

    public partial PeriodCapRuleResource ToPeriodCapRuleResource(PeriodCapRule rule);

    [MapperIgnoreTarget(nameof(PeriodCapRule.CreateTime))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.UpdateTime))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.DeletedTime))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.IsDeleted))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.ImportSourceKey))]
    [MapperIgnoreTarget(nameof(PeriodCapRule.ImportContentHash))]
    public partial PeriodCapRule ToPeriodCapRuleEntity(PeriodCapRuleResource resource);

    public void UpdatePeriodCapRuleEntity(PeriodCapRule target, PeriodCapRuleResource source)
    {
        target.Period = source.Period;
        target.Scope = source.Scope;
        target.CapHours = source.CapHours;
        target.WarnAtPercent = source.WarnAtPercent;
        target.CustomPeriodWeeks = source.CustomPeriodWeeks;
        target.RollingWindowWeeks = source.RollingWindowWeeks;
        target.MaxAverageWeeklyHours = source.MaxAverageWeeklyHours;
        target.SchedulingRuleId = source.SchedulingRuleId;
    }

    public partial RestrictedTimeWindowRuleResource ToRestrictedTimeWindowRuleResource(RestrictedTimeWindowRule rule);

    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.CreateTime))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.UpdateTime))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.DeletedTime))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.IsDeleted))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.ImportSourceKey))]
    [MapperIgnoreTarget(nameof(RestrictedTimeWindowRule.ImportContentHash))]
    public partial RestrictedTimeWindowRule ToRestrictedTimeWindowRuleEntity(RestrictedTimeWindowRuleResource resource);

    public void UpdateRestrictedTimeWindowRuleEntity(RestrictedTimeWindowRule target, RestrictedTimeWindowRuleResource source)
    {
        target.SeasonFromMonth = source.SeasonFromMonth;
        target.SeasonFromDay = source.SeasonFromDay;
        target.SeasonToMonth = source.SeasonToMonth;
        target.SeasonToDay = source.SeasonToDay;
        target.DailyStart = source.DailyStart;
        target.DailyEnd = source.DailyEnd;
        target.AppliesToGroupTag = source.AppliesToGroupTag;
    }
}
