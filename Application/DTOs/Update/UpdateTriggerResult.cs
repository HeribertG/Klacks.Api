// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateTriggerResult
{
    public bool Enqueued { get; set; }

    public Guid? OperationId { get; set; }

    public string Reason { get; set; } = string.Empty;
}
