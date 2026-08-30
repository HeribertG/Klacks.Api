// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ApproveProposedSkillChangeCommand : IRequest<LearningMutationResult>
{
    public Guid ProposalId { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
}
