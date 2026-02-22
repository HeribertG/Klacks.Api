// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

public sealed record ClientCreatedEvent(Guid ClientId, string Name) : DomainEvent;
