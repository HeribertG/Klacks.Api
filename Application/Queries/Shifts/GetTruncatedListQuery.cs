// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Shifts;

public record GetTruncatedListQuery(ShiftFilter Filter) : IRequest<TruncatedShiftResource>;
