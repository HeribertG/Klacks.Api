// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a calendar selection, identified by id or by name (staged fuzzy resolution,
/// disambiguated instead of guessed). Goes through DeleteCommand, which refuses seeded system
/// calendars and calendars still referenced by a group, a contract or the global setting — the
/// resulting error names the concrete blocker. The delete is verified by confirming the row no
/// longer appears in the database; a mismatch rolls the delete back.
/// </summary>
/// <param name="calendarSelectionId">Optional. UUID of the calendar selection to delete; takes
/// precedence over calendarSelectionName.</param>
/// <param name="calendarSelectionName">Optional. Name of the calendar selection to delete; used
/// to resolve it when calendarSelectionId is omitted.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class DeleteCalendarSelectionSkill : BaseSkillImplementation
{
    private const string SkillName = "delete_calendar_selection";

    private readonly IMediator _mediator;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCalendarSelectionSkill(
        IMediator mediator,
        ICalendarSelectionRepository calendarSelectionRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _calendarSelectionRepository = calendarSelectionRepository;
        _unitOfWork = unitOfWork;
    }

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

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _mediator.Send(new DeleteCommand<CalendarSelectionResource>(selection.Id), cancellationToken);

                var selectionId = selection.Id;
                await ConfirmDeletedAsync(
                    SkillName,
                    () => _calendarSelectionRepository.GetNoTracking(selectionId),
                    $"calendar selection '{selection.Name}'");

                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return SkillResult.Error(ex.Message);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        return SkillResult.SuccessResult(
            new { CalendarSelectionId = selection.Id, DeletedName = selection.Name },
            $"Calendar selection '{selection.Name}' was soft-deleted and confirmed removed from the database (verified).");
    }
}
