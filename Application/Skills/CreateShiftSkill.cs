// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill for creating ONE single order (Bestellung) in the system. Owner decision 2026-10-24: the order is always created
/// as an open order (status OriginalOrder) and never sealed on create - an asDraft argument is ignored. Sealing it is a
/// separate step through seal_shift, which runs only as the confirmed replay of its server preview; only the sealed order
/// gets the plannable shift that can be booked and cut. Every order MUST be billed to a customer, so a valid clientId of a
/// customer (EntityTypeEnum.Customer) is required. A multi-part 24h service is modelled as ONE order with the full span
/// (e.g. 07:00-07:00) and is cut into parts via <see cref="CutShiftSkill"/> once sealed - never by calling this skill
/// several times. A structurally identical open order, or the plannable shift of an identical sealed one, is reused
/// instead of creating a duplicate (ORD-5a).
/// A client hidden from the caller by group visibility is answered exactly like an unknown client id.
/// </summary>
/// <param name="name">Name of the shift (e.g. "Early shift Bern")</param>
/// <param name="clientId">UUID of the customer (EntityTypeEnum.Customer) the order is billed to; mandatory.</param>
/// <param name="startTime">Start time of the shift (e.g. "07:00")</param>
/// <param name="endTime">End time of the shift (e.g. "15:00"); may equal startTime for a 24h order</param>
/// <param name="macroId">Optional UUID of the calculation macro; defaults to the standard macro (category Shift, function Standard).
/// A macro created by the assistant (origin Assistant or AssistantExtension) is refused via <see cref="MacroAssistantGuard"/>.</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_shift")]
public class CreateShiftSkill : BaseSkillImplementation
{
    private const string MacroIdParameter = "macroId";
    private const string MacroNotFoundMessage =
        "Calculation macro with ID {0} not found. Use list_macros to pick a valid macro.";
    private const string MissingClientIdMessage =
        "Cannot create the order yet. Every order (Bestellung) must be billed to a customer. " +
        "Ask the user which customer this order is for — list existing customers with find_customer_candidates, " +
        "or create a new one with create_employee using entityType=Customer — then call create_shift again with clientId.";
    private const string NoShiftMacroMessage =
        "Cannot create the order yet. No calculation macro with category 'Shift' is configured, so the " +
        "order would have no way to compute its hours and surcharges. First set up a calculation macro " +
        "with category Shift (function Standard) — inspect the existing macros with list_macros or " +
        "configure one in Settings — then call create_shift again (optionally pass its id as macroId).";

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDefaultShiftMacroResolver _defaultShiftMacroResolver;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public CreateShiftSkill(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        IDefaultShiftMacroResolver defaultShiftMacroResolver,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _groupVisibilityGuard = groupVisibilityGuard;
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _defaultShiftMacroResolver = defaultShiftMacroResolver;
    }

    private sealed record OrderInput(
        string Name,
        string Abbreviation,
        string StartTimeText,
        string EndTimeText,
        string? FromDateText,
        string? UntilDateText,
        int SumEmployees,
        int Quantity,
        string? GroupIdsRaw,
        string Description,
        string? ClientIdText,
        string WeekdaysText);

    private sealed record OrderSchedule(
        TimeOnly StartTime,
        TimeOnly EndTime,
        DateOnly FromDate,
        DateOnly? UntilDate,
        decimal WorkTime);

    private sealed record OrderBilling(Guid ClientId, string ClientName, Guid? MacroId, string? MacroName);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var input = ReadInput(parameters);

        var (clientId, clientName, clientError) = await ResolveCustomerAsync(input.ClientIdText, cancellationToken);
        if (clientName is null)
        {
            return SkillResult.Error(clientError!);
        }

        var (macroId, macroName, macroError) = await ResolveMacroAsync(parameters, cancellationToken);
        if (macroError != null)
        {
            return SkillResult.Error(macroError);
        }

        var (schedule, scheduleError) = ParseSchedule(input, context.UserLanguage);
        if (schedule is null)
        {
            return SkillResult.Error(scheduleError!);
        }

        var (groupItems, groupError) = await ResolveGroupItemsAsync(input.GroupIdsRaw, schedule.FromDate, cancellationToken);
        if (groupItems is null)
        {
            return SkillResult.Error(groupError!);
        }

        var billing = new OrderBilling(clientId, clientName, macroId, macroName);
        var shift = BuildOrder(input, schedule, billing, groupItems, context.UserName);

        var reusable = await _shiftRepository.FindReusableUncutOrderAsync(shift, cancellationToken);
        if (reusable != null)
        {
            return ReusedOrderResult(reusable, input.Name, billing);
        }

        var resultShift = await _shiftRepository.AddWithSealedOrderHandling(shift);
        await _unitOfWork.CompleteAsync();

        return CreatedOrderResult(input, schedule, billing, groupItems, shift, resultShift);
    }

    private static OrderInput ReadInput(Dictionary<string, object> parameters)
    {
        var name = GetRequiredString(parameters, "name");
        var abbreviation = GetParameter<string>(parameters, "abbreviation") ?? GenerateAbbreviation(name);
        var startTimeStr = GetRequiredString(parameters, "startTime");
        var endTimeStr = GetRequiredString(parameters, "endTime");

        return new OrderInput(
            name,
            abbreviation,
            startTimeStr,
            endTimeStr,
            GetParameter<string>(parameters, "fromDate"),
            GetParameter<string>(parameters, "untilDate"),
            GetParameter<int>(parameters, "sumEmployees", 1),
            GetParameter<int>(parameters, "quantity", 1),
            GetParameter<string>(parameters, "groupIds"),
            GetParameter<string>(parameters, "description") ?? "",
            GetParameter<string>(parameters, "clientId"),
            GetParameter<string>(parameters, "weekdays") ?? "all");
    }

    private async Task<(Guid ClientId, string? ClientName, string? Error)> ResolveCustomerAsync(
        string? clientIdStr, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientIdStr) || !Guid.TryParse(clientIdStr, out var clientId))
        {
            return (Guid.Empty, null, MissingClientIdMessage);
        }

        var client = await LoadVisibleCustomerAsync(clientId, cancellationToken);
        if (client == null)
        {
            return (clientId, null, $"Customer with ID {clientId} not found.");
        }

        var clientName = !string.IsNullOrWhiteSpace(client.Company)
            ? client.Company
            : $"{client.FirstName} {client.Name}".Trim();

        if (client.Type != EntityTypeEnum.Customer)
        {
            return (clientId, null,
                $"Client '{clientName}' is not a customer (type {client.Type}). An order must be billed to a customer. " +
                "Pick a customer with find_customer_candidates or create one with create_employee using entityType=Customer.");
        }

        return (clientId, clientName, null);
    }

    private static (OrderSchedule? Schedule, string? Error) ParseSchedule(OrderInput input, string? language)
    {
        var startTimeStr = input.StartTimeText;
        var endTimeStr = input.EndTimeText;
        var fromDateStr = input.FromDateText;
        var untilDateStr = input.UntilDateText;

        if (!SkillCalendarStringParser.TryParseTimeOnly(startTimeStr, language, out var startTime))
        {
            return (null, $"Invalid start time format: {startTimeStr}. Expected HH:mm");
        }

        if (!SkillCalendarStringParser.TryParseTimeOnly(endTimeStr, language, out var endTime))
        {
            return (null, $"Invalid end time format: {endTimeStr}. Expected HH:mm");
        }

        if (string.IsNullOrWhiteSpace(fromDateStr))
        {
            return (null,
                "Cannot create the order yet. Ask the user from when the order is valid (ab wann / von wann gilt die Bestellung). " +
                "When you ask, append a date picker so the user can pick it instead of typing: [REPLIES:date \"Ab wann gilt die Bestellung?\"]. " +
                "Resolve casual phrases the user already gave you: \"heute\"/\"sofort\"/\"today\" -> today's date; " +
                "\"morgen\"/\"tomorrow\" -> tomorrow's date; \"1. Juli\"/\"July 1st\" -> the matching YYYY-MM-DD. " +
                "Then call create_shift again with fromDate in YYYY-MM-DD format.");
        }

        if (!SkillCalendarStringParser.TryParseDateOnly(fromDateStr, language, out var fromDate))
        {
            return (null, $"Invalid fromDate format: {fromDateStr}. Expected YYYY-MM-DD (e.g. 2026-06-01).");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrEmpty(untilDateStr) && SkillCalendarStringParser.TryParseDateOnly(untilDateStr, language, out var parsedUntil))
        {
            untilDate = parsedUntil;
        }

        if (untilDate.HasValue && untilDate.Value < fromDate)
        {
            return (null, $"untilDate ({untilDate.Value.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}) must not be before fromDate ({fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}).");
        }

        if (input.SumEmployees < 1)
        {
            return (null, "sumEmployees (required staff count) must be at least 1.");
        }

        if (input.Quantity < 1)
        {
            return (null, "quantity must be at least 1.");
        }

        return (new OrderSchedule(startTime, endTime, fromDate, untilDate, CalculateWorkTime(startTime, endTime)), null);
    }

    private async Task<(List<GroupItem>? GroupItems, string? Error)> ResolveGroupItemsAsync(
        string? groupIdsRaw, DateOnly fromDate, CancellationToken cancellationToken)
    {
        var groupItems = new List<GroupItem>();
        if (!string.IsNullOrEmpty(groupIdsRaw))
        {
            var groupIdStrings = groupIdsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var gidStr in groupIdStrings)
            {
                if (Guid.TryParse(gidStr, out var gid))
                {
                    var group = await _groupRepository.Get(gid);
                    if (group == null || !await _groupVisibilityGuard.IsGroupVisibleAsync(gid, cancellationToken))
                    {
                        return (null, $"Group with ID {gid} not found.");
                    }

                    groupItems.Add(new GroupItem
                    {
                        GroupId = gid,
                        ValidFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    });
                }
            }
        }

        return (groupItems, null);
    }

    private static Shift BuildOrder(
        OrderInput input, OrderSchedule schedule, OrderBilling billing, List<GroupItem> groupItems, string userName)
    {
        var (isMonday, isTuesday, isWednesday, isThursday, isFriday, isSaturday, isSunday) =
            ParseWeekdays(input.WeekdaysText);

        return new Shift
        {
            Id = Guid.NewGuid(),
            Name = input.Name,
            Abbreviation = input.Abbreviation,
            Description = input.Description,
            ClientId = billing.ClientId,
            MacroId = billing.MacroId,
            Status = ShiftStatus.OriginalOrder,
            ShiftType = ShiftType.IsTask,
            IsTimeRange = true,
            StartShift = schedule.StartTime,
            EndShift = schedule.EndTime,
            FromDate = schedule.FromDate,
            UntilDate = schedule.UntilDate,
            WorkTime = schedule.WorkTime,
            SumEmployees = input.SumEmployees,
            Quantity = input.Quantity,
            IsMonday = isMonday,
            IsTuesday = isTuesday,
            IsWednesday = isWednesday,
            IsThursday = isThursday,
            IsFriday = isFriday,
            IsSaturday = isSaturday,
            IsSunday = isSunday,
            GroupItems = groupItems,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = userName
        };
    }

    private static SkillResult ReusedOrderResult(Shift reusable, string name, OrderBilling billing)
    {
        var clientName = billing.ClientName;
        var data = new
        {
            ShiftId = reusable.Id,
            reusable.Name,
            Reused = true,
            ClientId = billing.ClientId,
            ClientName = clientName
        };

        if (reusable.Status == ShiftStatus.OriginalOrder)
        {
            return SkillResult.SuccessResult(
                data,
                $"Order '{name}' already exists as an open order for customer '{clientName}' (shiftId=\"{reusable.Id}\") — " +
                "no new order created. It is not sealed yet, so it cannot be booked or cut. " + SealStepHint(reusable.Id));
        }

        return SkillResult.SuccessResult(
            data,
            $"Order '{name}' already exists for customer '{clientName}' (not yet split) — no new order created. " +
            $"If it should be split into parts, call cut_shift now with shiftId=\"{reusable.Id}\" and the requested parts " +
            "(e.g. parts=\"07:00-15:00,15:00-23:00,23:00-07:00\", partNames=\"Frühdienst,Spätdienst,Nachtdienst\"). " +
            "Do NOT navigate to the cut page and do NOT write manual cutting instructions.");
    }

    private static string SealStepHint(Guid orderId) =>
        "Klacksy never seals an order on create. Once name, abbreviation, fromDate, at least one weekday or holiday, at " +
        "least one group, quantity>0 and sumEmployees>0 are all set, call seal_shift with " +
        $"shiftId=\"{orderId}\": the user confirms its preview, and only the sealed order gets the plannable shift that " +
        "can be booked - and split with cut_shift if it should be cut into parts.";

    private static SkillResult CreatedOrderResult(
        OrderInput input,
        OrderSchedule schedule,
        OrderBilling billing,
        List<GroupItem> groupItems,
        Shift shift,
        Shift resultShift)
    {
        var name = input.Name;
        var startTime = schedule.StartTime;
        var endTime = schedule.EndTime;
        var workTime = schedule.WorkTime;
        var clientName = billing.ClientName;
        var macroName = billing.MacroName;

        var resultData = new
        {
            ShiftId = resultShift.Id,
            shift.Name,
            shift.Abbreviation,
            StartTime = startTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
            EndTime = endTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture),
            FromDate = schedule.FromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            UntilDate = schedule.UntilDate?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            WorkTime = workTime,
            SumEmployees = input.SumEmployees,
            GroupCount = groupItems.Count,
            ClientId = billing.ClientId,
            ClientName = clientName,
            MacroId = billing.MacroId,
            MacroName = macroName
        };

        var macroInfo = macroName != null
            ? $" with calculation macro '{macroName}'"
            : " (no default calculation macro configured — set one with category Shift)";

        var message =
            $"Order '{name}' ({startTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture)}-" +
            $"{endTime.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture)}, {workTime}h) created as an open order " +
            $"(status OriginalOrder) for customer '{clientName}'{macroInfo}, with {groupItems.Count} group(s) — NOT sealed, so " +
            "it has no plannable shift yet and cannot be booked or cut. " + SealStepHint(resultShift.Id);

        return SkillResult.SuccessResult(resultData, message);
    }

    private async Task<Client?> LoadVisibleCustomerAsync(Guid clientId, CancellationToken cancellationToken)
    {
        if (!await _clientVisibilityGuard.IsVisibleAsync(clientId, cancellationToken))
        {
            return null;
        }

        return (await _clientRepository.GetByIdsAsync(new[] { clientId }, cancellationToken)).FirstOrDefault();
    }

    private async Task<(Guid? Id, string? Name, string? Error)> ResolveMacroAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();
        var macroIdStr = GetParameter<string>(parameters, MacroIdParameter);
        if (!string.IsNullOrWhiteSpace(macroIdStr) && Guid.TryParse(macroIdStr, out var parsedMacroId))
        {
            var requestedMacro = macros.FirstOrDefault(m => m.Id == parsedMacroId);
            if (requestedMacro == null)
            {
                return (null, null, string.Format(CultureInfo.InvariantCulture, MacroNotFoundMessage, parsedMacroId));
            }

            var assignmentRefusal = MacroAssistantGuard.FindAssignmentRefusal(requestedMacro);
            return assignmentRefusal != null
                ? (null, null, assignmentRefusal)
                : (requestedMacro.Id, requestedMacro.Name, null);
        }

        var defaultMacroId = await _defaultShiftMacroResolver.ResolveDefaultMacroIdAsync(cancellationToken);
        return defaultMacroId == null
            ? (null, null, NoShiftMacroMessage)
            : (defaultMacroId, macros.FirstOrDefault(m => m.Id == defaultMacroId)?.Name, null);
    }

    private static (bool mon, bool tue, bool wed, bool thu, bool fri, bool sat, bool sun) ParseWeekdays(string weekdays)
    {
        var value = weekdays.Trim().ToLowerInvariant();

        switch (value)
        {
            case "" or "all":
                return (true, true, true, true, true, true, true);
            case "weekdays":
                return (true, true, true, true, true, false, false);
            case "weekend":
                return (false, false, false, false, false, true, true);
        }

        var days = new bool[CalendarConstants.DaysPerWeek];
        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = CanonicalDayIndex(token);
            if (index >= 0)
            {
                days[index] = true;
            }
        }

        return (
            days[(int)ShiftDayType.Monday],
            days[(int)ShiftDayType.Tuesday],
            days[(int)ShiftDayType.Wednesday],
            days[(int)ShiftDayType.Thursday],
            days[(int)ShiftDayType.Friday],
            days[(int)ShiftDayType.Saturday],
            days[(int)ShiftDayType.Sunday]);
    }

    private static int CanonicalDayIndex(string token) => token switch
    {
        "mon" or "monday" => (int)ShiftDayType.Monday,
        "tue" or "tuesday" => (int)ShiftDayType.Tuesday,
        "wed" or "wednesday" => (int)ShiftDayType.Wednesday,
        "thu" or "thursday" => (int)ShiftDayType.Thursday,
        "fri" or "friday" => (int)ShiftDayType.Friday,
        "sat" or "saturday" => (int)ShiftDayType.Saturday,
        "sun" or "sunday" => (int)ShiftDayType.Sunday,
        _ => -1
    };
}
