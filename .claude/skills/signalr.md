# SignalR

## WorkNotificationHub

- **Pfad:** `/hubs/work-notifications`
- **Authentifizierung:** JWT Bearer (via `access_token` Query-Parameter)

## Events

| Event | Beschreibung | Payload |
|-------|--------------|---------|
| `WorkCreated` | Work erstellt | `WorkNotificationDto` |
| `WorkUpdated` | Work aktualisiert | `WorkNotificationDto` |
| `WorkDeleted` | Work gelöscht | `WorkNotificationDto` |
| `ShiftStatsUpdated` | Shift-Statistiken | `ShiftStatsNotificationDto` |
| `PeriodHoursUpdated` | Period Hours | `PeriodHoursNotificationDto` |
| `PeriodHoursRecalculated` | Bulk Recalc | `{ StartDate, EndDate }` |

## Sender-Ausschluss

Frontend sendet ConnectionId im Header:
```
X-SignalR-ConnectionId: <connectionId>
```

Backend schließt Sender vom Broadcast aus.

## Group-Based Broadcasting

Nachrichten nur an Clients im gleichen Zeitraum.

### Group-Naming
```
schedule_{startDate}_{endDate}
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
```

## Dateien

**Backend:**
- `Infrastructure/Hubs/WorkNotificationHub.cs`
- `Infrastructure/Hubs/IWorkNotificationService.cs`
- `Infrastructure/Hubs/WorkNotificationService.cs`
- `Presentation/DTOs/Notifications/*.cs`

**Frontend:**
- `infrastructure/signalr/signalr.service.ts`
- `presentation/auth/auth.interceptor.ts` (ConnectionId Header)
