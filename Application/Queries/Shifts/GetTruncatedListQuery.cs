// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetTruncatedListQuery(ShiftFilter Filter) : IRequest<TruncatedShiftResource>;
