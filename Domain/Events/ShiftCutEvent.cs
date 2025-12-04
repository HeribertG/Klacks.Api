namespace Klacks.Api.Domain.Events;

public sealed record ShiftCutEvent(Guid RootShiftId, int ChildCount) : DomainEvent;
