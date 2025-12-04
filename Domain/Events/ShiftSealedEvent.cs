namespace Klacks.Api.Domain.Events;

public sealed record ShiftSealedEvent(Guid ShiftId, Guid? ClientId) : DomainEvent;
