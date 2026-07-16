// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared field validation for the counter rule Post/Put commands.
/// </summary>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.CounterRules;

internal static class CounterRuleValidation
{
    public static void Validate(CounterRuleResource? resource)
    {
        if (resource == null)
        {
            throw new InvalidRequestException("Counter rule data is required.");
        }

        if (!Enum.IsDefined(resource.EventType))
        {
            throw new InvalidRequestException("EventType must be a defined counter event type.");
        }

        if (!Enum.IsDefined(resource.Period))
        {
            throw new InvalidRequestException("Period must be a defined counter period.");
        }

        if (resource.Threshold <= 0)
        {
            throw new InvalidRequestException("Threshold must be greater than zero.");
        }

        if (resource.EventType == CounterEventType.ShiftExceedingHours &&
            (resource.HoursThreshold == null || resource.HoursThreshold <= 0))
        {
            throw new InvalidRequestException("HoursThreshold is required and must be greater than zero when EventType is ShiftExceedingHours.");
        }

        if (resource.Enforcement.HasValue && !Enum.IsDefined(resource.Enforcement.Value))
        {
            throw new InvalidRequestException("Enforcement must be a defined enforcement mode or null.");
        }
    }
}
