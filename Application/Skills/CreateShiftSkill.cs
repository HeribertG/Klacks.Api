// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill for creating new shifts/tasks in the system.
/// </summary>
/// <param name="name">Name of the shift (e.g. "Early shift Bern")</param>
/// <param name="startTime">Start time of the shift (e.g. "07:00")</param>
/// <param name="endTime">End time of the shift (e.g. "15:00")</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_shift")]
public class CreateShiftSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateShiftSkill(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
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
        var groupIdsRaw = GetParameter<string>(parameters, "groupIds");
        var description = GetParameter<string>(parameters, "description") ?? "";

        var weekdaysStr = GetParameter<string>(parameters, "weekdays") ?? "all";

        if (!TimeOnly.TryParse(startTimeStr, out var startTime))
        {
            return SkillResult.Error($"Invalid start time format: {startTimeStr}. Expected HH:mm");
        }

        if (!TimeOnly.TryParse(endTimeStr, out var endTime))
        {
            return SkillResult.Error($"Invalid end time format: {endTimeStr}. Expected HH:mm");
        }

        var fromDate = DateOnly.FromDateTime(DateTime.Today);
        if (!string.IsNullOrEmpty(fromDateStr) && DateOnly.TryParse(fromDateStr, out var parsedFrom))
        {
            fromDate = parsedFrom;
        }

        DateOnly? untilDate = null;
        if (!string.IsNullOrEmpty(untilDateStr) && DateOnly.TryParse(untilDateStr, out var parsedUntil))
        {
            untilDate = parsedUntil;
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
            Status = ShiftStatus.SealedOrder,
            ShiftType = ShiftType.IsTask,
            StartShift = startTime,
            EndShift = endTime,
            FromDate = fromDate,
            UntilDate = untilDate,
            WorkTime = workTime,
            SumEmployees = sumEmployees,
            Quantity = 1,
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
            GroupCount = groupItems.Count
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Shift '{name}' ({startTime:HH:mm}-{endTime:HH:mm}, {workTime}h) created successfully with {groupItems.Count} group(s).");
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
