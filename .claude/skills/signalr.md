---
name: signalr
description: "Verwende wenn an SignalR-Hubs, Echtzeit-Benachrichtigungen oder WebSocket-Events im Backend gearbeitet wird"
---

# SignalR

## Hubs

| Hub | Pfad | Beschreibung |
|-----|------|--------------|
| WorkNotificationHub | `/hubs/work-notifications` | Arbeitszeit-Benachrichtigungen |
| EmailNotificationHub | `/hubs/email-notifications` | E-Mail-Benachrichtigungen |
| AssistantNotificationHub | `/hubs/assistant-notifications` | LLM-Assistent proaktive Nachrichten |

**Authentifizierung:** JWT Bearer (via `access_token` Query-Parameter)

## WorkNotificationHub Events (9 Events)

| Event | Beschreibung | Payload |
|-------|--------------|---------|
| `WorkCreated` | Work erstellt | `WorkNotificationDto` |
| `WorkUpdated` | Work aktualisiert | `WorkNotificationDto` |
| `WorkDeleted` | Work gelöscht | `WorkNotificationDto` |
| `ScheduleUpdated` | Schedule geändert | Schedule-Daten |
| `ShiftStatsUpdated` | Shift-Statistiken | `ShiftStatsNotificationDto` |
| `PeriodHoursUpdated` | Period Hours | `PeriodHoursNotificationDto` |
| `PeriodHoursRecalculated` | Bulk Recalc | `{ StartDate, EndDate }` |
| `ScheduleChangeTracked` | Änderungsverfolgung | Change-Tracking-Daten |
| `CollisionsDetected` | Kollisionen erkannt | Kollisionsdaten |

## EmailNotificationHub Events

| Event | Beschreibung |
|-------|--------------|
| `NewEmailsReceived` | Neue E-Mails eingegangen |
| `EmailReadStateChanged` | Lesestatus geändert |

## AssistantNotificationHub Events

| Event | Beschreibung |
|-------|--------------|
| `ProactiveMessage` | Proaktive Nachricht vom Assistenten |
| `OnboardingPrompt` | Onboarding-Aufforderung |

## Sender-Ausschluss

Frontend sendet ConnectionId im Header:
```
X-SignalR-ConnectionId: <connectionId>
```

Backend schließt Sender vom Broadcast aus.

## Group-Based Broadcasting

Nachrichten nur an Clients im gleichen Zeitraum oder für den gleichen Client.

### Group-Naming

```
schedule_{startDate}_{endDate}
client_{clientId}
```

### Server-Methoden

| Methode | Parameter | Beschreibung |
|---------|-----------|--------------|
| `JoinScheduleGroup` | startDate, endDate | Gruppe beitreten |
| `LeaveScheduleGroup` | startDate, endDate | Gruppe verlassen |
| `GetConnectionId` | - | ConnectionId abrufen |

### Hub Implementation

```csharp
public async Task JoinScheduleGroup(string startDate, string endDate)
{
    var groupName = $"schedule_{startDate}_{endDate}";
    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
}
```

## DTOs

### WorkNotificationDto

```csharp
public record WorkNotificationDto
{
    public Guid WorkId { get; init; }
    public Guid ClientId { get; init; }
    public Guid ShiftId { get; init; }
    public DateTime CurrentDate { get; init; }
    public DateOnly PeriodStartDate { get; init; }
    public DateOnly PeriodEndDate { get; init; }
    public string Action { get; init; }
    public string SourceConnectionId { get; init; }
}
```

### PeriodHoursNotificationDto

```csharp
public record PeriodHoursNotificationDto
{
    public Guid ClientId { get; init; }
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public decimal Hours { get; init; }
    public decimal Surcharges { get; init; }
    public decimal GuaranteedHours { get; init; }
}
```

## Registrierung

```csharp
// Program.cs
builder.Services.AddSignalR();
builder.Services.AddScoped<IWorkNotificationService, WorkNotificationService>();

app.MapHub<WorkNotificationHub>("/hubs/work-notifications");
app.MapHub<EmailNotificationHub>("/hubs/email-notifications");
app.MapHub<AssistantNotificationHub>("/hubs/assistant-notifications");
```

## Dateien

**Backend:**
- `Infrastructure/Hubs/WorkNotificationHub.cs`
- `Infrastructure/Hubs/EmailNotificationHub.cs`
- `Infrastructure/Hubs/AssistantNotificationHub.cs`
- `Infrastructure/Hubs/IWorkNotificationService.cs`
- `Infrastructure/Hubs/WorkNotificationService.cs`
- `Presentation/DTOs/Notifications/*.cs`

**Frontend:**
- `infrastructure/signalr/signalr.service.ts`
- `presentation/auth/auth.interceptor.ts` (ConnectionId Header)
