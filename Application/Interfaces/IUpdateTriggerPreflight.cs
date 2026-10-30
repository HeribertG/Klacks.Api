// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Update;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Read-only decision whether an update could be enqueued right now. The trigger command, its confirmation preview and
/// the update advice share it, so none of them can offer what the trigger refuses.
/// </summary>
public interface IUpdateTriggerPreflight
{
    Task<UpdateTriggerPreflightResult> CheckAsync(CancellationToken cancellationToken = default);
}
