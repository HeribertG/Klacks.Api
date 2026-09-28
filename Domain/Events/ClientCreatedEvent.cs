// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Events;

public sealed record ClientCreatedEvent(Guid ClientId, string Name) : DomainEvent;
