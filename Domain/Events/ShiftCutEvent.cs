// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Events;

public sealed record ShiftCutEvent(Guid RootShiftId, int ChildCount) : DomainEvent;
