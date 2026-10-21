// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates an existing calendar selection: rename it, merge in more country/subdivision
/// calendars, remove merged calendars, and set or clear the per-calendar "reminder only"
/// override (downgrades holidays to non-official within this selection without touching the
/// underlying country/subdivision rules globally). The target selection is identified by id or
/// by name (staged fuzzy resolution, disambiguated instead of guessed). Only the fields actually
/// supplied are changed — with nothing supplied this is a no-op success. The full merged
/// calendar list is sent through PutCommand (the update service matches existing rows by
/// country/state, so a partial payload would silently drop calendars that are not repeated) and
/// the write is verified by re-reading the row straight from the database; a mismatch rolls the
/// update back.
/// </summary>
/// <param name="calendarSelectionId">Optional. UUID of the calendar selection to update; takes
/// precedence over calendarSelectionName.</param>
/// <param name="calendarSelectionName">Optional. Current name of the calendar selection; used to
/// resolve it when calendarSelectionId is omitted.</param>
/// <param name="newName">Optional. New display name.</param>
/// <param name="addCalendars">Optional. Calendar tokens to merge in (country code for national,
/// e.g. "US", or "COUNTRY-STATE" for a subdivision, e.g. "CH-BE").</param>
/// <param name="removeCalendars">Optional. Calendar tokens to remove from the selection.</param>
/// <param name="reminderOnlyCalendars">Optional. Tokens (already in the selection, or added in
/// the same call) whose holidays should be downgraded to reminder-only.</param>
/// <param name="inheritCalendars">Optional. Tokens whose reminder-only override should be
/// cleared, so they go back to inheriting the normal official/unofficial status.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class UpdateCalendarSelectionSkill : BaseSkillImplementation
{
    private const string SkillName = "update_calendar_selection";

    private readonly IMediator _mediator;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ISettingsTokenService _settingsTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCalendarSelectionSkill(
        IMediator mediator,
        ICalendarSelectionRepository calendarSelectionRepository,
        ISettingsTokenService settingsTokenService,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _calendarSelectionRepository = calendarSelectionRepository;
        _settingsTokenService = settingsTokenService;
        _unitOfWork = unitOfWork;
    }

    private sealed record OverrideKeys(HashSet<(string, string)> ReminderKeys, HashSet<(string, string)> InheritKeys);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var all = (await _calendarSelectionRepository.List()).Where(c => !c.IsDeleted).ToList();

        var (selection, resolveError) = CalendarSelectionResolver.ResolveFromParameters(
            GetParameter<string>(parameters, "calendarSelectionId"),
            GetParameter<string>(parameters, "calendarSelectionName"),
            all);
        if (selection == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var availableTokens = (await _settingsTokenService.GetRuleTokenListAsync(false)).ToList();

        var merged = selection.SelectedCalendars
            .ToDictionary(sc => (sc.Country, sc.State), sc => sc.OfficialOverride);
        var changed = new List<string>();

        var finalName = ApplyNewName(parameters, selection, changed);

        var editError = ApplyCalendarEdits(parameters, availableTokens, merged, changed);
        if (editError != null)
        {
            return SkillResult.Error(editError);
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { CalendarSelectionId = selection.Id, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — calendar selection left unchanged.");
        }

        if (merged.Count == 0)
        {
            return SkillResult.Error(
                $"Calendar selection '{selection.Name}' must keep at least one calendar — refusing to remove the last one.");
        }

        var writeError = await UpdateAndConfirmAsync(selection, finalName, merged, changed, cancellationToken);
        if (writeError != null)
        {
            return SkillResult.Error(writeError);
        }

        return BuildSuccessResult(selection, finalName, merged, changed);
    }

    private static string ApplyNewName(
        Dictionary<string, object> parameters, CalendarSelection selection, List<string> changed)
    {
        var newName = GetParameter<string>(parameters, "newName");
        var finalName = selection.Name;
        if (!string.IsNullOrWhiteSpace(newName) && newName.Trim() != selection.Name)
        {
            finalName = newName.Trim();
            changed.Add("name");
        }

        return finalName;
    }

    private static string? ApplyCalendarEdits(
        Dictionary<string, object> parameters,
        List<StateCountryToken> availableTokens,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed)
    {
        var removeError = RemoveCalendars(parameters, availableTokens, merged, changed);
        if (removeError != null)
        {
            return removeError;
        }

        var addError = AddCalendars(parameters, availableTokens, merged, changed);
        if (addError != null)
        {
            return addError;
        }

        var (overrideKeys, overrideError) = ResolveOverrideKeys(parameters, availableTokens);
        if (overrideKeys is null)
        {
            return overrideError;
        }

        return ApplyReminderOnly(overrideKeys.ReminderKeys, merged, changed)
            ?? ApplyInherit(overrideKeys.InheritKeys, merged, changed);
    }

    private static string? RemoveCalendars(
        Dictionary<string, object> parameters,
        List<StateCountryToken> availableTokens,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed)
    {
        var removeRaw = SkillStringListParser.ParseStringList(parameters, "removeCalendars") ?? new();
        foreach (var raw in removeRaw)
        {
            var (country, state, error) = CalendarTokenParser.Resolve(raw, availableTokens);
            if (error != null)
            {
                return error;
            }

            if (merged.Remove((country, state)))
            {
                changed.Add($"removed {CalendarTokenParser.Format(country, state)}");
            }
        }

        return null;
    }

    private static string? AddCalendars(
        Dictionary<string, object> parameters,
        List<StateCountryToken> availableTokens,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed)
    {
        var addRaw = SkillStringListParser.ParseStringList(parameters, "addCalendars") ?? new();
        foreach (var raw in addRaw)
        {
            var (country, state, error) = CalendarTokenParser.Resolve(raw, availableTokens);
            if (error != null)
            {
                return error;
            }

            if (!merged.ContainsKey((country, state)))
            {
                merged[(country, state)] = null;
                changed.Add($"added {CalendarTokenParser.Format(country, state)}");
            }
        }

        return null;
    }

    private static (OverrideKeys? Keys, string? Error) ResolveOverrideKeys(
        Dictionary<string, object> parameters, List<StateCountryToken> availableTokens)
    {
        var reminderRaw = SkillStringListParser.ParseStringList(parameters, "reminderOnlyCalendars") ?? new();
        var inheritRaw = SkillStringListParser.ParseStringList(parameters, "inheritCalendars") ?? new();

        var reminderKeys = new HashSet<(string, string)>();
        foreach (var raw in reminderRaw)
        {
            var (country, state, error) = CalendarTokenParser.Resolve(raw, availableTokens);
            if (error != null)
            {
                return (null, error);
            }

            reminderKeys.Add((country, state));
        }

        var inheritKeys = new HashSet<(string, string)>();
        foreach (var raw in inheritRaw)
        {
            var (country, state, error) = CalendarTokenParser.Resolve(raw, availableTokens);
            if (error != null)
            {
                return (null, error);
            }

            if (reminderKeys.Contains((country, state)))
            {
                return (null,
                    $"'{raw}' is listed in both reminderOnlyCalendars and inheritCalendars — pick one.");
            }

            inheritKeys.Add((country, state));
        }

        return (new OverrideKeys(reminderKeys, inheritKeys), null);
    }

    private static string? ApplyReminderOnly(
        HashSet<(string, string)> reminderKeys,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed)
    {
        foreach (var key in reminderKeys)
        {
            if (!merged.TryGetValue(key, out var currentOverride))
            {
                return
                    $"reminderOnlyCalendars entry '{CalendarTokenParser.Format(key.Item1, key.Item2)}' is not part " +
                    "of this calendar selection — add it via addCalendars first.";
            }

            if (currentOverride != false)
            {
                merged[key] = false;
                changed.Add($"reminder-only {CalendarTokenParser.Format(key.Item1, key.Item2)}");
            }
        }

        return null;
    }

    private static string? ApplyInherit(
        HashSet<(string, string)> inheritKeys,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed)
    {
        foreach (var key in inheritKeys)
        {
            if (!merged.TryGetValue(key, out var currentOverride))
            {
                return
                    $"inheritCalendars entry '{CalendarTokenParser.Format(key.Item1, key.Item2)}' is not part of " +
                    "this calendar selection.";
            }

            if (currentOverride != null)
            {
                merged[key] = null;
                changed.Add($"inherit {CalendarTokenParser.Format(key.Item1, key.Item2)}");
            }
        }

        return null;
    }

    private static CalendarSelectionResource BuildResource(
        CalendarSelection selection, string finalName, Dictionary<(string Country, string State), bool?> merged)
    {
        return new CalendarSelectionResource
        {
            Id = selection.Id,
            Name = finalName,
            SelectedCalendars = merged.Select(kv => new SelectedCalendarResource
            {
                Id = Guid.Empty,
                CalendarSelectionId = Guid.Empty,
                Country = kv.Key.Item1,
                State = kv.Key.Item2,
                OfficialOverride = kv.Value
            }).ToList()
        };
    }

    private async Task<string?> UpdateAndConfirmAsync(
        CalendarSelection selection,
        string finalName,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed,
        CancellationToken cancellationToken)
    {
        var resource = BuildResource(selection, finalName, merged);

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var updated = await _mediator.Send(new PutCommand<CalendarSelectionResource>(resource), cancellationToken);
                if (updated == null)
                {
                    throw new SkillVerificationException(SkillName, $"Calendar selection '{finalName}' could not be updated.");
                }

                var selectionId = selection.Id;
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _calendarSelectionRepository.GetNoTrackingWithSelectedCalendars(selectionId),
                    persisted => !persisted.IsDeleted
                        && persisted.Name == finalName
                        && persisted.SelectedCalendars.Count == merged.Count
                        && merged.All(kv => persisted.SelectedCalendars.Any(sc =>
                            sc.Country == kv.Key.Item1 && sc.State == kv.Key.Item2 && sc.OfficialOverride == kv.Value)),
                    $"the update ({string.Join(", ", changed)}) of calendar selection '{finalName}'");

                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return ex.Message;
        }

        return null;
    }

    private static SkillResult BuildSuccessResult(
        CalendarSelection selection,
        string finalName,
        Dictionary<(string Country, string State), bool?> merged,
        List<string> changed)
    {
        return SkillResult.SuccessResult(
            new
            {
                CalendarSelectionId = selection.Id,
                ChangedFields = changed,
                Name = finalName,
                Calendars = merged.Select(kv => new { Country = kv.Key.Item1, State = kv.Key.Item2, OfficialOverride = kv.Value }).ToList()
            },
            $"Calendar selection '{finalName}' updated ({string.Join(", ", changed)}) and confirmed in the database (verified).");
    }
}
