// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// A single problem or note attached to one day in a billing period.
/// </summary>
/// <param name="Date">Date the issue applies to</param>
/// <param name="ClientId">Client this issue belongs to, empty when global</param>
/// <param name="ClientName">Display name of the client</param>
/// <param name="Severity">Error / Warning / Info classification</param>
/// <param name="Code">Stable identifier for the rule that produced this issue</param>
/// <param name="MessageKey">i18n key the frontend can translate</param>
/// <param name="MessageParams">Optional placeholders for the translated message</param>
public class PeriodIssueDto
{
    public DateOnly Date { get; set; }

    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    [JsonConverter(typeof(JsonStringEnumConverter<ScheduleValidationType>))]
    public ScheduleValidationType Severity { get; set; }

    public string Code { get; set; } = string.Empty;

    public string MessageKey { get; set; } = string.Empty;

    public Dictionary<string, string> MessageParams { get; set; } = new();
}
