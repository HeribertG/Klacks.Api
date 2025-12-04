using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public interface IShiftCuttingService
{
    bool CanBeCut(Shift shift);
    Shift CreateCutChild(Shift parent, DateOnly fromDate, DateOnly? untilDate, TimeOnly startShift, TimeOnly endShift);
    void ValidateCutOperation(Shift parent, DateOnly fromDate, DateOnly? untilDate);
}

public class ShiftCuttingService : IShiftCuttingService
{
    private readonly IShiftTreeService _shiftTreeService;
    private readonly ILogger<ShiftCuttingService> _logger;
    private readonly List<IDomainEvent> _domainEvents = [];

    public ShiftCuttingService(
        IShiftTreeService shiftTreeService,
        ILogger<ShiftCuttingService> logger)
    {
        _shiftTreeService = shiftTreeService;
        _logger = logger;
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool CanBeCut(Shift shift)
    {
        return shift.Status == ShiftStatus.SealedOrder ||
               shift.Status == ShiftStatus.OriginalShift ||
               shift.Status == ShiftStatus.SplitShift;
    }

    public Shift CreateCutChild(Shift parent, DateOnly fromDate, DateOnly? untilDate, TimeOnly startShift, TimeOnly endShift)
    {
        if (!CanBeCut(parent))
            throw new InvalidOperationException(
                $"Shift with status {parent.Status} cannot be cut.");

        var child = new Shift
        {
            Id = Guid.NewGuid(),
            Status = ShiftStatus.SplitShift,
            OriginalId = parent.OriginalId ?? parent.Id,

            Name = parent.Name,
            Abbreviation = parent.Abbreviation,
            Description = parent.Description,

            ClientId = parent.ClientId,
            MacroId = parent.MacroId,

            FromDate = fromDate,
            UntilDate = untilDate,
            StartShift = startShift,
            EndShift = endShift,

            BeforeShift = parent.BeforeShift,
            AfterShift = parent.AfterShift,
            BriefingTime = parent.BriefingTime,
            DebriefingTime = parent.DebriefingTime,
            TravelTimeBefore = parent.TravelTimeBefore,
            TravelTimeAfter = parent.TravelTimeAfter,

            IsMonday = parent.IsMonday,
            IsTuesday = parent.IsTuesday,
            IsWednesday = parent.IsWednesday,
            IsThursday = parent.IsThursday,
            IsFriday = parent.IsFriday,
            IsSaturday = parent.IsSaturday,
            IsSunday = parent.IsSunday,
            IsHoliday = parent.IsHoliday,
            IsWeekdayOrHoliday = parent.IsWeekdayOrHoliday,

            IsSporadic = parent.IsSporadic,
            SporadicScope = parent.SporadicScope,
            IsTimeRange = parent.IsTimeRange,

            Quantity = parent.Quantity,
            SumEmployees = parent.SumEmployees,
            WorkTime = parent.WorkTime,

            ShiftType = parent.ShiftType
        };

        if (parent.Status == ShiftStatus.SealedOrder || parent.Status == ShiftStatus.OriginalShift)
        {
            _shiftTreeService.SetHierarchyRelation(child, null, null);
        }
        else
        {
            _shiftTreeService.SetHierarchyRelation(child, parent.Id, parent.RootId);
        }

        _logger.LogInformation("Created cut child {ChildId} from parent {ParentId}",
            child.Id, parent.Id);

        return child;
    }

    public void ValidateCutOperation(Shift parent, DateOnly fromDate, DateOnly? untilDate)
    {
        if (fromDate < parent.FromDate)
            throw new ArgumentException(
                $"Cut from date {fromDate} cannot be before parent from date {parent.FromDate}");

        if (untilDate.HasValue && parent.UntilDate.HasValue && untilDate.Value > parent.UntilDate.Value)
            throw new ArgumentException(
                $"Cut until date {untilDate} cannot be after parent until date {parent.UntilDate}");

        if (untilDate.HasValue && untilDate.Value < fromDate)
            throw new ArgumentException(
                $"Cut until date {untilDate} cannot be before from date {fromDate}");
    }
}
