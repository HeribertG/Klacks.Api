namespace Klacks.Api.Domain.Events;

public sealed record ShiftCreatedEvent(Guid ShiftId, Guid? ClientId) : DomainEvent;
