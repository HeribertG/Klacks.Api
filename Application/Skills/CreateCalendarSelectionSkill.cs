// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a new calendar selection (a named holiday calendar that can merge one or more
/// country/subdivision holiday calendars into one). Any merged calendar can optionally be
/// marked "reminder only", which downgrades its holidays to non-official within this selection
/// (they still show up, but do not count for payroll/surcharge calculation) without touching the
/// underlying country/subdivision rules globally. The write goes through PostCommand so
/// validation, mapping and the holiday-calculator cache invalidation stay centralized in the
/// existing handler, and is verified by re-reading the created row (with its calendars) straight
/// from the database; a mismatch rolls the write back.
/// </summary>
/// <param name="name">Required. Display name of the new calendar selection.</param>
/// <param name="calendars">Required. Calendar tokens to merge: a country code for the national
/// calendar (e.g. "US") or "COUNTRY-STATE" for a subdivision (e.g. "CH-BE").</param>
/// <param name="reminderOnlyCalendars">Optional. Subset of <c>calendars</c> whose holidays are
/// downgraded to reminder-only (shown, but excluded from payroll calculation) within this
/// selection.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class CreateCalendarSelectionSkill : BaseSkillImplementation
{
    private const string SkillName = "create_calendar_selection";

    private readonly IMediator _mediator;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ISettingsTokenService _settingsTokenService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCalendarSelectionSkill(
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

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name").Trim();
        if (name.Length == 0)
        {
            return SkillResult.Error("Parameter 'name' must not be empty.");
        }

        var rawCalendars = SkillStringListParser.ParseStringList(parameters, "calendars") ?? new();
        if (rawCalendars.Count == 0)
        {
            return SkillResult.Error(
                "Parameter 'calendars' is required and must contain at least one calendar token " +
                "(a country code like 'US' or 'COUNTRY-STATE' like 'CH-BE').");
        }

        var rawReminderOnly = SkillStringListParser.ParseStringList(parameters, "reminderOnlyCalendars") ?? new();

        var availableTokens = (await _settingsTokenService.GetRuleTokenListAsync(false)).ToList();

        var tokens = new List<(string Country, string State)>();
        var seenKeys = new HashSet<(string, string)>();
        foreach (var raw in rawCalendars)
        {
            var (country, state, error) = CalendarTokenParser.Resolve(raw, availableTokens);
            if (error != null)
            {
                return SkillResult.Error(error);
            }

            if (seenKeys.Add((country, state)))
            {
                tokens.Add((country, state));
            }
        }

        var reminderKeys = new HashSet<(string, string)>();
        foreach (var raw in rawReminderOnly)
        {
            var (country, state, error) = CalendarTokenParser.Resolve(raw, availableTokens);
            if (error != null)
            {
                return SkillResult.Error(error);
            }

            if (!seenKeys.Contains((country, state)))
            {
                return SkillResult.Error(
                    $"reminderOnlyCalendars entry '{raw}' is not part of 'calendars' — add it to calendars first.");
            }

            reminderKeys.Add((country, state));
        }

        var resource = new CalendarSelectionResource
        {
            Id = Guid.Empty,
            Name = name,
            SelectedCalendars = tokens.Select(t => new SelectedCalendarResource
            {
                Id = Guid.Empty,
                CalendarSelectionId = Guid.Empty,
                Country = t.Country,
                State = t.State,
                OfficialOverride = reminderKeys.Contains((t.Country, t.State)) ? false : null
            }).ToList()
        };

        CalendarSelectionResource? created = null;
        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                created = await _mediator.Send(new PostCommand<CalendarSelectionResource>(resource), cancellationToken);
                if (created == null)
                {
                    throw new SkillVerificationException(SkillName, $"Calendar selection '{name}' could not be created.");
                }

                var createdId = created.Id;
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _calendarSelectionRepository.GetNoTrackingWithSelectedCalendars(createdId),
                    persisted => !persisted.IsDeleted
                        && persisted.Name == name
                        && persisted.SelectedCalendars.Count == tokens.Count
                        && tokens.All(t => persisted.SelectedCalendars.Any(sc =>
                            sc.Country == t.Country
                            && sc.State == t.State
                            && sc.OfficialOverride == (reminderKeys.Contains((t.Country, t.State)) ? false : (bool?)null))),
                    $"the new calendar selection '{name}'");

                return createdId;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var reminderNote = reminderKeys.Count > 0
            ? $" ({reminderKeys.Count} marked reminder-only)"
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                CalendarSelectionId = created!.Id,
                created.Name,
                Calendars = tokens.Select(t => new { t.Country, t.State }).ToList(),
                ReminderOnlyCount = reminderKeys.Count
            },
            $"Calendar selection '{name}' created with {tokens.Count} calendar(s){reminderNote} and confirmed in the database (verified).");
    }
}
