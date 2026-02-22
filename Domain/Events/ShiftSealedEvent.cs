// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

public sealed record ShiftSealedEvent(Guid ShiftId, Guid? ClientId) : DomainEvent;
