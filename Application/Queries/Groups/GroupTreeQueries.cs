// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record GetGroupTreeQuery(Guid? RootId, bool ApplyVisibilityScope = false) : IRequest<GroupTreeResource>;
