// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Assistant.Learning;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ApproveProposedSkillChangeCommand : IRequest<LearningMutationResult>
{
    public Guid ProposalId { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
}
