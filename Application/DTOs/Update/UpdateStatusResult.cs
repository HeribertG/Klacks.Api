// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.DTOs.Update;

public class UpdateStatusResult
{
    public string CurrentVersion { get; set; } = string.Empty;

    public UpdateAvailability? Availability { get; set; }

    public UpdateHistoryItem? ActiveOperation { get; set; }

    public UpdateHistoryItem? LastOperation { get; set; }
}
