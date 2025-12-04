using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public interface IShiftSealingService
{
    Task<Shift> SealShiftAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<Shift> CreateOriginalShiftFromSealedAsync(Shift sealedShift, CancellationToken cancellationToken = default);
    bool CanBeSeal(Shift shift);
    bool IsSealedOrLater(Shift shift);
}

public class ShiftSealingService : IShiftSealingService
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ILogger<ShiftSealingService> _logger;
    private readonly List<IDomainEvent> _domainEvents = [];

    public ShiftSealingService(
        IShiftRepository shiftRepository,
        ILogger<ShiftSealingService> logger)
    {
        _shiftRepository = shiftRepository;
        _logger = logger;
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public async Task<Shift> SealShiftAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await _shiftRepository.Get(shiftId);

        if (shift == null)
            throw new KeyNotFoundException($"Shift with ID {shiftId} not found");

        if (!CanBeSeal(shift))
            throw new InvalidOperationException(
                $"Shift with status {shift.Status} cannot be sealed. Only OriginalOrder (Status=0) can be sealed.");

        shift.Status = ShiftStatus.SealedOrder;

        _logger.LogInformation("Sealed shift {ShiftId}, Status changed to {Status}",
            shift.Id, shift.Status);

        _domainEvents.Add(new ShiftSealedEvent(shift.Id, shift.ClientId));

        return shift;
    }

    public async Task<Shift> CreateOriginalShiftFromSealedAsync(Shift sealedShift, CancellationToken cancellationToken = default)
    {
        if (sealedShift.Status != ShiftStatus.SealedOrder)
            throw new InvalidOperationException(
                $"Cannot create OriginalShift from shift with status {sealedShift.Status}. Must be SealedOrder.");

        var originalShift = new Shift
        {
            Id = Guid.NewGuid(),
            OriginalId = sealedShift.Id,
            Status = ShiftStatus.OriginalShift,

            Name = sealedShift.Name,
            Abbreviation = sealedShift.Abbreviation,
            Description = sealedShift.Description,

            ClientId = sealedShift.ClientId,
            MacroId = sealedShift.MacroId,

            FromDate = sealedShift.FromDate,
            UntilDate = sealedShift.UntilDate,
            StartShift = sealedShift.StartShift,
            EndShift = sealedShift.EndShift,

            BeforeShift = sealedShift.BeforeShift,
            AfterShift = sealedShift.AfterShift,
            BriefingTime = sealedShift.BriefingTime,
            DebriefingTime = sealedShift.DebriefingTime,
            TravelTimeBefore = sealedShift.TravelTimeBefore,
            TravelTimeAfter = sealedShift.TravelTimeAfter,

            IsMonday = sealedShift.IsMonday,
            IsTuesday = sealedShift.IsTuesday,
            IsWednesday = sealedShift.IsWednesday,
            IsThursday = sealedShift.IsThursday,
            IsFriday = sealedShift.IsFriday,
            IsSaturday = sealedShift.IsSaturday,
            IsSunday = sealedShift.IsSunday,
            IsHoliday = sealedShift.IsHoliday,
            IsWeekdayOrHoliday = sealedShift.IsWeekdayOrHoliday,

            IsSporadic = sealedShift.IsSporadic,
            SporadicScope = sealedShift.SporadicScope,
            IsTimeRange = sealedShift.IsTimeRange,

            Quantity = sealedShift.Quantity,
            SumEmployees = sealedShift.SumEmployees,
            WorkTime = sealedShift.WorkTime,

            ShiftType = sealedShift.ShiftType
        };

        await _shiftRepository.Add(originalShift);

        _logger.LogInformation("Created OriginalShift {NewShiftId} from SealedOrder {SealedId}",
            originalShift.Id, sealedShift.Id);

        _domainEvents.Add(new ShiftCreatedEvent(originalShift.Id, originalShift.ClientId));

        return originalShift;
    }

    public bool CanBeSeal(Shift shift)
    {
        return shift.Status == ShiftStatus.OriginalOrder;
    }

    public bool IsSealedOrLater(Shift shift)
    {
        return shift.Status >= ShiftStatus.SealedOrder;
    }
}
