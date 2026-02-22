// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Queries.Assistant;

public record GetSkillByNameQuery(string Name) : IRequest<SkillDto?>;
