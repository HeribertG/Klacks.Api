// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill for creating ONE single shift/order (Bestellung) in the system. Every order MUST be billed
/// to a customer, so a valid clientId of a customer (EntityTypeEnum.Customer) is required. A multi-part
/// 24h service is modelled as ONE order with the full span (e.g. 07:00-07:00) and is afterwards cut
/// into parts via <see cref="CutShiftSkill"/> — never by calling this skill several times.
/// </summary>
/// <param name="name">Name of the shift (e.g. "Early shift Bern")</param>
/// <param name="clientId">UUID of the customer (EntityTypeEnum.Customer) the order is billed to; mandatory.</param>
/// <param name="startTime">Start time of the shift (e.g. "07:00")</param>
/// <param name="endTime">End time of the shift (e.g. "15:00"); may equal startTime for a 24h order</param>
/// <param name="macroId">Optional UUID of the calculation macro; defaults to the standard macro (the one with category Shift).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_shift")]
public class CreateShiftSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShiftSkill(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IClientRepository clientRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _clientRepository = clientRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name");
        var abbreviation = GetParameter<string>(parameters, "abbreviation") ?? GenerateAbbreviation(name);
        var startTimeStr = GetRequiredString(parameters, "startTime");
        var endTimeStr = GetRequiredString(parameters, "endTime");
        var fromDateStr = GetParameter<string>(parameters, "fromDate");
        var untilDateStr = GetParameter<string>(parameters, "untilDate");
        var sumEmployees = GetParameter<int>(parameters, "sumEmployees", 1);
        var quantity = GetParameter<int>(parameters, "quantity", 1);
        var groupIdsRaw = GetParameter<string>(parameters, "groupIds");
        var description = GetParameter<string>(parameters, "description") ?? "";

        var clientIdStr = GetParameter<string>(parameters, "clientId");
        if (string.IsNullOrWhiteSpace(clientIdStr) || !Guid.TryParse(clientIdStr, out var clientId))
        {
            return SkillResult.Error(
                "Cannot create the order yet. Every order (Bestellung) must be billed to a customer. " +
                "Ask the user which customer this order is for — list existing customers with find_customer_candidates, " +
                "or create a new one with create_employee using entityType=Customer — then call create_shift again with clientId.");
        }

        var client = (await _clientRepository.GetByIdsAsync(new[] { clientId })).FirstOrDefault();
        if (client == null)
        {
            return SkillResult.Error($"Customer with ID {clientId} not found.");
        }

        var clientName = !string.IsNullOrWhiteSpace(client.Company)
            ? client.Company
            : $"{client.FirstName} {client.Name}".Trim();

        if (client.Type != EntityTypeEnum.Customer)
        {
            return SkillResult.Error(
                $"Client '{clientName}' is not a customer (type {client.Type}). An order must be billed to a customer. " +
                "Pick a customer with find_customer_candidates or create one with create_employee using entityType=Customer.");
        }

        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();
        var macroIdStr = GetParameter<string>(parameters, "macroId");
        Guid? macroId;
        if (!string.IsNullOrWhiteSpace(macroIdStr) && Guid.TryParse(macroIdStr, out var parsedMacroId))
        {
            if (macros.All(m => m.Id != parsedMacroId))
            {
                return SkillResult.Error(
                    $"Calculation macro with ID {parsedMacroId} not found. Use list_macros to pick a valid macro.");
            }

            macroId = parsedMacroId;
        }
        else
        {
            // No macro given: apply the standard default — the macro whose category is Shift.
            macroId = macros.FirstOrDefault(m => m.Category == MacroCategoryEnum.Shift)?.Id;
        }

        var macroName = macros.FirstOrDefault(m => m.Id == macroId)?.Name;

        var weekdaysStr = GetParameter<string>(parameters, "weekdays") ?? "all";

        if (!TimeOnly.TryParse(startTimeStr, out var startTime))
        {
            return SkillResult.Error($"Invalid start time format: {startTimeStr}. Expected HH:mm");
        }

        if (!TimeOnly.TryParse(endTimeStr, out var endTime))
        {
            return SkillResult.Error($"Invalid end time format: {endTimeStr}. Expected HH:mm");
        }

        if (string.IsNullOrWhiteSpace(fromDateStr))
        {
            return SkillResult.Error(
                "Cannot create the order yet. Ask the user from when the order is valid (ab wann / von wann gilt die Bestellung). " +
                "When you ask, append a date picker so the user can pick it instead of typing: [REPLIES:date \"Ab wann gilt die Bestellung?\"]. " +
                "Resolve casual phrases the user already gave you: \"heute\"/\"sofort\"/\"today\" -> today's date; " +
                "\"morgen\"/\"tomorrow\" -> tomorrow's date; \"1. Juli\"/\"July 1st\" -> the matching YYYY-MM-DD. " +
                "Then call create_shift again with fromDate in YYYY-MM-DD format.");
        }

        if (!DateOnly.TryParse(fromDateStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate format: {fromDateStr}. Expected YYYY-MM-DD (e.g. 2026-06-01).");
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrEmpty(untilDateStr) && DateOnly.TryParse(untilDateStr, out var parsedUntil))
        {
            untilDate = parsedUntil;
        }

        if (untilDate.HasValue && untilDate.Value < fromDate)
        {
            return SkillResult.Error(
                $"untilDate ({untilDate:yyyy-MM-dd}) must not be before fromDate ({fromDate:yyyy-MM-dd}).");
        }

        if (sumEmployees < 1)
        {
            return SkillResult.Error("sumEmployees (required staff count) must be at least 1.");
        }

        if (quantity < 1)
        {
            return SkillResult.Error("quantity must be at least 1.");
        }

        var (isMonday, isTuesday, isWednesday, isThursday, isFriday, isSaturday, isSunday) =
            ParseWeekdays(weekdaysStr);

        var workTime = CalculateWorkTime(startTime, endTime);

        var groupItems = new List<GroupItem>();
        if (!string.IsNullOrEmpty(groupIdsRaw))
        {
            var groupIdStrings = groupIdsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var gidStr in groupIdStrings)
            {
                if (Guid.TryParse(gidStr, out var gid))
                {
                    var group = await _groupRepository.Get(gid);
                    if (group == null)
                    {
                        return SkillResult.Error($"Group with ID {gid} not found.");
                    }

                    groupItems.Add(new GroupItem
                    {
                        GroupId = gid,
                        ValidFrom = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                    });
                }
            }
        }

        var shift = new Shift
        {
            Id = Guid.NewGuid(),
            Name = name,
            Abbreviation = abbreviation,
            Description = description,
            ClientId = clientId,
            MacroId = macroId,
            Status = ShiftStatus.SealedOrder,
            ShiftType = ShiftType.IsTask,
            StartShift = startTime,
            EndShift = endTime,
            FromDate = fromDate,
            UntilDate = untilDate,
            WorkTime = workTime,
            SumEmployees = sumEmployees,
            Quantity = quantity,
            IsMonday = isMonday,
            IsTuesday = isTuesday,
            IsWednesday = isWednesday,
            IsThursday = isThursday,
            IsFriday = isFriday,
            IsSaturday = isSaturday,
            IsSunday = isSunday,
            GroupItems = groupItems,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        // ORD-5a idempotency: reuse a structurally identical uncut order instead of creating a duplicate
        // (the deterministic guard lives in the repository — UI/REST stays unaffected). On a hit, route
        // the model to cut the existing order instead of re-creating.
        var reusable = await _shiftRepository.FindReusableUncutOrderAsync(shift, cancellationToken);
        if (reusable != null)
        {
            return SkillResult.SuccessResult(
                new
                {
                    ShiftId = reusable.Id,
                    SealedOrderId = reusable.OriginalId,
                    reusable.Name,
                    Reused = true,
                    ClientId = clientId,
                    ClientName = clientName
                },
                $"Order '{name}' already exists for customer '{clientName}' (not yet split) — no new order created. " +
                "Use this ShiftId to cut it into the requested parts with cut_shift.");
        }

        var resultShift = await _shiftRepository.AddWithSealedOrderHandling(shift);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            ShiftId = resultShift.Id,
            SealedOrderId = shift.Id,
            shift.Name,
            shift.Abbreviation,
            StartTime = startTime.ToString("HH:mm"),
            EndTime = endTime.ToString("HH:mm"),
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            UntilDate = untilDate?.ToString("yyyy-MM-dd"),
            WorkTime = workTime,
            SumEmployees = sumEmployees,
            GroupCount = groupItems.Count,
            ClientId = clientId,
            ClientName = clientName,
            MacroId = macroId,
            MacroName = macroName
        };

        var macroInfo = macroName != null
            ? $" with calculation macro '{macroName}'"
            : " (no default calculation macro configured — set one with category Shift)";

        return SkillResult.SuccessResult(
            resultData,
            $"Order '{name}' ({startTime:HH:mm}-{endTime:HH:mm}, {workTime}h) created for customer '{clientName}'{macroInfo}, with {groupItems.Count} group(s).");
    }

    private static string GenerateAbbreviation(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
        {
            return name.Length >= 3 ? name[..3].ToUpper() : name.ToUpper();
        }

        return string.Concat(parts.Select(p => char.ToUpper(p[0])));
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

        var days = new bool[7];
        foreach (var token in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var index = CanonicalDayIndex(token);
            if (index >= 0)
            {
                days[index] = true;
            }
        }

        return (days[0], days[1], days[2], days[3], days[4], days[5], days[6]);
    }

    private static int CanonicalDayIndex(string token) => token switch
    {
        "mon" or "monday" => 0,
        "tue" or "tuesday" => 1,
        "wed" or "wednesday" => 2,
        "thu" or "thursday" => 3,
        "fri" or "friday" => 4,
        "sat" or "saturday" => 5,
        "sun" or "sunday" => 6,
        _ => -1
    };
}
