// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

﻿using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.GroupVisibilities;

public record GroupVisibilityListQuery(string Id) : IRequest<IEnumerable<GroupVisibilityResource>>;
