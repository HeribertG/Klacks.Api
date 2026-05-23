// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public record GetPendingSkillChangesQuery(int Limit) : IRequest<IReadOnlyList<ProposedSkillChange>>;
