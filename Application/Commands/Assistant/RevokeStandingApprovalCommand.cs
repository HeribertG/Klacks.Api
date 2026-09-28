// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Withdraws a standing approval. False means there was nothing to withdraw - an unknown id or a grant
/// somebody else revoked first.
/// </summary>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record RevokeStandingApprovalCommand(Guid Id, Guid RevokedByUserId) : IRequest<bool>;
