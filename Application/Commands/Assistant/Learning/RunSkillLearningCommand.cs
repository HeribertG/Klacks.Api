// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant.Learning;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant.Learning;

public sealed record RunSkillLearningCommand : IRequest<SkillLearningRunResponse>;
