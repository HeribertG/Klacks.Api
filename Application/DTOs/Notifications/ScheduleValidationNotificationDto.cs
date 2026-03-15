// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Einzelner Validierungseintrag für die Schedule-Fehlerliste.
/// Kann Error (Kollision), Warning (Ruhezeit, Arbeitszeit) oder Info (Unterbesetzung) sein.
/// </summary>
/// <param name="Type">error, warning oder info</param>
/// <param name="ClientId">Betroffener Mitarbeiter</param>
/// <param name="ClientName">Name des Mitarbeiters</param>
/// <param name="Date">Datum des Eintrags</param>
/// <param name="Comment">Übersetzungsschlüssel für die Anzeige</param>
/// <param name="CommentParams">Parameter für die Übersetzung</param>
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleValidationNotificationDto
{
    [JsonConverter(typeof(JsonStringEnumConverter<ScheduleValidationType>))]
    public ScheduleValidationType Type { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string Comment { get; init; } = string.Empty;
    public Dictionary<string, string> CommentParams { get; init; } = new();
}
