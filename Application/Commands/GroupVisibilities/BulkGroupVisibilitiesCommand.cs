// Copyright (c) Heribert Gasparoli Private. All rights reserved.

﻿using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.GroupVisibilities;

public record BulkGroupVisibilitiesCommand(List<GroupVisibilityResource> List) : IRequest;