// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Liste aller Schedule-Validierungseinträge für das Frontend.
/// Enthält Kollisionen, Ruhezeit-Verletzungen, Überstunden und Unterbesetzung.
/// </summary>
/// <param name="Entries">Alle Validierungseinträge</param>
/// <param name="IsFullRefresh">True bei erstmaligem Laden, ersetzt alle bestehenden Einträge</param>
/// <param name="CheckedClientId">Geprüfter Client bei Einzel-Check</param>
/// <param name="CheckedDate">Geprüftes Datum bei Einzel-Check</param>
namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleValidationListNotificationDto
{
    public List<ScheduleValidationNotificationDto> Entries { get; init; } = [];
    public bool IsFullRefresh { get; init; }
    public Guid? CheckedClientId { get; init; }
    public DateOnly? CheckedDate { get; init; }
}
