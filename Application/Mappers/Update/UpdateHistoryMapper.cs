// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Projects an UpdateHistory entity to its API-facing DTO, rendering enums as stable string names.
/// </summary>
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Application.Mappers.Update;

public static class UpdateHistoryMapper
{
    public static UpdateHistoryItem ToItem(UpdateHistory entry)
    {
        return new UpdateHistoryItem
        {
            Id = entry.Id,
            OperationType = entry.OperationType.ToString(),
            Status = entry.Status.ToString(),
            Channel = entry.Channel.ToString(),
            FromVersion = entry.FromVersion,
            TargetVersion = entry.TargetVersion,
            ContainsMigrations = entry.ContainsMigrations,
            RequestedAt = entry.RequestedAt,
            StartedAt = entry.StartedAt,
            CompletedAt = entry.CompletedAt,
            Message = entry.Message,
        };
    }
}
