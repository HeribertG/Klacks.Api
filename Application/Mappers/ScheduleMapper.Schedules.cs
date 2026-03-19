// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Partielle Klasse für Schedule-, WorkChange-, Expenses-, Notes- und SchedulingRule-Mappings.
/// </summary>
using Klacks.Api.Domain.Common;
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
    public partial ShiftScheduleResource ToShiftScheduleResource(ShiftDayAssignment assignment);

    public partial List<ShiftScheduleResource> ToShiftScheduleResourceList(List<ShiftDayAssignment> assignments);

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
    [MapperIgnoreTarget(nameof(ScheduleNote.AnalyseToken))]
    public partial ScheduleNote ToScheduleNoteEntity(ScheduleNoteResource resource);

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
        target.SaRate = source.SaRate;
        target.SoRate = source.SoRate;
    }
}
