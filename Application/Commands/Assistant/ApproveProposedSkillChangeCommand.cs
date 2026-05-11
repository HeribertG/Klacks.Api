// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ApproveProposedSkillChangeCommand : IRequest<ApproveProposedSkillChangeResult>
{
    public Guid ProposalId { get; set; }
    public string ReviewedBy { get; set; } = string.Empty;
}

public sealed record ApproveProposedSkillChangeResult(bool Applied, string? Error, int? NewSkillVersion);
