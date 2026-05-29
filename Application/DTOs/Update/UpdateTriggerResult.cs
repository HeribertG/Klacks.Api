// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateTriggerResult
{
    public bool Enqueued { get; set; }

    public Guid? OperationId { get; set; }

    public string Reason { get; set; } = string.Empty;
}
