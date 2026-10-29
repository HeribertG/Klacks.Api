// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The container template edit lock as a skill holds it for one write. The chat and the open container page run in
/// the same browser tab, so they share user and instance id; acquiring then hands back the page's own lock
/// (AlreadyHeld). Disposing releases only a lock this scope created, so a skill never deletes the lock the page still
/// saves under. The release runs with CancellationToken.None and a failing release is swallowed, so it can never mask
/// the outcome of the write.
/// </summary>
/// <param name="mediator">Sends the acquire and release commands</param>
/// <param name="containerId">Container whose template lock is taken</param>
/// <param name="instanceId">Instance id of the caller (X-Instance-Id); empty when the request carries none</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class ContainerTemplateLockScope : IAsyncDisposable
{
    private readonly IMediator _mediator;
    private readonly ContainerLockResource _lock;

    private ContainerTemplateLockScope(IMediator mediator, ContainerLockResource containerLock)
    {
        _mediator = mediator;
        _lock = containerLock;
    }

    public bool Acquired => _lock.Acquired;

    public bool AlreadyHeld => _lock.AlreadyHeld;

    public static async Task<ContainerTemplateLockScope> AcquireAsync(
        IMediator mediator, Guid containerId, string instanceId, CancellationToken cancellationToken)
    {
        var containerLock = await mediator.Send(
            new AcquireContainerLockCommand(ContainerLockResourceTypes.ContainerTemplate, containerId, instanceId),
            cancellationToken);

        return new ContainerTemplateLockScope(mediator, containerLock);
    }

    public async ValueTask DisposeAsync()
    {
        if (!_lock.Acquired || _lock.AlreadyHeld)
        {
            return;
        }

        try
        {
            await _mediator.Send(new ReleaseContainerLockCommand(_lock.Id), CancellationToken.None);
        }
        catch (Exception)
        {
        }
    }
}
