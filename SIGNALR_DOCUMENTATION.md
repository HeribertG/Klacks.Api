# SignalR Documentation

**Letzte Aktualisierung:** 21.01.2026

---

## Übersicht

SignalR wird für Echtzeit-Benachrichtigungen zwischen Backend und Frontend verwendet. Alle Schedule-relevanten Events laufen über einen einzigen Hub.

---

## WorkNotificationHub

### Konfiguration

- **Pfad:** `/hubs/work-notifications`
- **Authentifizierung:** JWT Bearer (via `access_token` Query-Parameter)

### Events

| Event | Beschreibung | Payload |
|-------|--------------|---------|
| `WorkCreated` | Work wurde erstellt | `WorkNotificationDto` |
| `WorkUpdated` | Work wurde aktualisiert | `WorkNotificationDto` |
| `WorkDeleted` | Work wurde gelöscht | `WorkNotificationDto` |
| `ShiftStatsUpdated` | Shift-Statistiken aktualisiert | `ShiftStatsNotificationDto` |
| `PeriodHoursUpdated` | Period Hours für Client aktualisiert | `PeriodHoursNotificationDto` |
| `PeriodHoursRecalculated` | Alle Period Hours neu berechnet | `{ StartDate, EndDate }` |

### Header für Sender-Ausschluss

```
X-SignalR-ConnectionId: <connectionId>
```

Der Sender erhält keine eigene Notification. Das Frontend sendet die eigene ConnectionId im Header, damit das Backend den Sender von der Broadcast-Liste ausschließen kann.

---

## Group-Based Broadcasting

### Konzept

Statt Nachrichten an ALLE Clients zu senden, werden Nachrichten nur an Clients gesendet, die denselben Zeitraum anzeigen. Dies verbessert die Performance bei vielen gleichzeitigen Benutzern erheblich.

### Group-Naming

```
schedule_{startDate}_{endDate}
```

Beispiel: `schedule_2026-01-01_2026-01-31`

### Server-Methoden

| Methode | Parameter | Beschreibung |
|---------|-----------|--------------|
| `JoinScheduleGroup` | `startDate`, `endDate` | Client tritt einer Gruppe bei |
| `LeaveScheduleGroup` | `startDate`, `endDate` | Client verlässt eine Gruppe |
| `GetConnectionId` | - | Gibt die ConnectionId zurück |

### Automatischer Ablauf

1. Client verbindet sich mit SignalR
2. Beim Laden des Schedules ruft `WorkScheduleLoaderService` → `joinScheduleGroup(periodStart, periodEnd)` auf
3. Client erhält nur Notifications für Works im gleichen Zeitraum
4. Bei Perioden-Wechsel: automatisch alte Gruppe verlassen, neue Gruppe beitreten
5. Bei Reconnect: automatisches Rejoinen der letzten Gruppe

### Backend-Implementation

```csharp
// Hub-Methoden
public async Task JoinScheduleGroup(string startDate, string endDate)
{
    var groupName = GetScheduleGroupName(startDate, endDate);
    await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
}

public async Task LeaveScheduleGroup(string startDate, string endDate)
{
    var groupName = GetScheduleGroupName(startDate, endDate);
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
}

// Statische Helper-Methode für Group-Name
public static string GetScheduleGroupName(DateOnly startDate, DateOnly endDate)
{
    return $"schedule_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";
}
```

### Frontend-Implementation

```typescript
// SignalRService
async joinScheduleGroup(startDate: string, endDate: string): Promise<void> {
  if (this.currentGroup) {
    await this.leaveScheduleGroup(this.currentGroup.startDate, this.currentGroup.endDate);
  }
  await this.hubConnection.invoke('JoinScheduleGroup', startDate, endDate);
  this.currentGroup = { startDate, endDate };
}

// Automatisches Rejoinen bei Reconnect
this.hubConnection.onreconnected(async () => {
  await this.rejoinCurrentGroup();
});
```

### WorkNotificationDto mit Period-Daten

```csharp
public record WorkNotificationDto
{
    public Guid WorkId { get; init; }
    public Guid ClientId { get; init; }
    public Guid ShiftId { get; init; }
    public DateTime CurrentDate { get; init; }
    public DateOnly PeriodStartDate { get; init; }  // NEU
    public DateOnly PeriodEndDate { get; init; }    // NEU
    public string OperationType { get; init; }
    public string SourceConnectionId { get; init; }
}
```

---

## DTOs

### WorkNotificationDto

```csharp
public record WorkNotificationDto
{
    public Guid WorkId { get; init; }
    public Guid ClientId { get; init; }
    public Guid ShiftId { get; init; }
    public DateTime CurrentDate { get; init; }
    public string Action { get; init; }           // "created", "updated", "deleted"
    public string SourceConnectionId { get; init; }
}
```

### ShiftStatsNotificationDto

```csharp
public record ShiftStatsNotificationDto
{
    public Guid ShiftId { get; init; }
    public DateOnly Date { get; init; }
    public int Engaged { get; init; }
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
    public string SourceConnectionId { get; init; }
}
```

---

## Frontend-Integration

### Verbindung herstellen

**Datei:** `infrastructure/signalr/signalr.service.ts`

```typescript
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl(`${environment.baseUrl}hubs/work-notifications`, {
    accessTokenFactory: () => this.authService.getToken()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
const connectionId = await connection.invoke<string>('GetConnectionId');
```

### Events abonnieren

```typescript
// Work Events
connection.on('WorkCreated', (notification: IWorkNotification) => {
  console.log('Work created:', notification);
});

connection.on('WorkUpdated', (notification: IWorkNotification) => {
  console.log('Work updated:', notification);
});

connection.on('WorkDeleted', (notification: IWorkNotification) => {
  console.log('Work deleted:', notification);
});

// Shift Stats Events
connection.on('ShiftStatsUpdated', (notification: IShiftStatsNotification) => {
  console.log('Shift stats updated:', notification);
});

// Period Hours Events
connection.on('PeriodHoursUpdated', (notification: IPeriodHoursNotification) => {
  console.log('Period hours updated:', notification);
});

connection.on('PeriodHoursRecalculated', (data: { startDate: string, endDate: string }) => {
  console.log('Period hours recalculated:', data);
});
```

### ConnectionId für API-Calls

```typescript
// Bei API-Calls mitsenden (via AuthInterceptor)
const connectionId = this.signalRService.connectionId;

this.http.post('/api/backend/Works', workData, {
  headers: { 'X-SignalR-ConnectionId': connectionId }
});
```

---

## Backend-Implementation

### Hub-Klasse

**Datei:** `Infrastructure/Hubs/WorkNotificationHub.cs`

```csharp
[Authorize]
public class WorkNotificationHub : Hub
{
    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}
```

### Notification Service Interface

**Datei:** `Infrastructure/Hubs/IWorkNotificationService.cs`

```csharp
public interface IWorkNotificationService
{
    Task NotifyWorkCreated(WorkNotificationDto notification);
    Task NotifyWorkUpdated(WorkNotificationDto notification);
    Task NotifyWorkDeleted(WorkNotificationDto notification);
    Task NotifyPeriodHoursUpdated(PeriodHoursNotificationDto notification);
    Task NotifyPeriodHoursRecalculated(DateOnly startDate, DateOnly endDate);
}
```

### Registrierung in Program.cs

```csharp
builder.Services.AddSignalR();
builder.Services.AddScoped<IWorkNotificationService, WorkNotificationService>();
builder.Services.AddScoped<IShiftStatsNotificationService, ShiftStatsNotificationService>();

// ...

app.MapHub<WorkNotificationHub>("/hubs/work-notifications");
```

---

## Datenfluss

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  User A erstellt/ändert/löscht Work (Period: Jan 2026)                       │
│  POST/PUT/DELETE /Works/{id}                                                 │
│  Header: X-SignalR-ConnectionId: "abc123"                                    │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  Handler (PostCommandHandler, etc.)                                          │
│  1. DB Operation                                                             │
│  2. PeriodHoursService.GetPeriodBoundaries() → (2026-01-01, 2026-01-31)    │
│  3. WorkNotificationDto mit PeriodStartDate/PeriodEndDate                   │
│  4. Notification an Group "schedule_2026-01-01_2026-01-31"                  │
│     └─► SignalR: WorkCreated/Updated/Deleted (excludes "abc123")            │
└───────────────────────────────────┬─────────────────────────────────────────┘
                                    │
          ┌─────────────────────────┼──────────────────────────┐
          │                         │                          │
          ▼                         ▼                          ▼
   ┌─────────────────┐    ┌─────────────────┐        ┌─────────────────┐
   │  User A         │    │  User B         │        │  User C         │
   │  (Sender)       │    │  (Jan 2026)     │        │  (Feb 2026)     │
   │  Jan 2026       │    │  Same Group     │        │  Other Group    │
   │                 │    │                 │        │                 │
   │  HTTP Response  │    │  ✓ SignalR      │        │  ✗ No Event     │
   └─────────────────┘    └─────────────────┘        └─────────────────┘
```

### Vorteile des Group-Based Broadcasting

- **Skalierbarkeit:** Auch mit 50+ Benutzern performant
- **Bandbreite:** Keine unnötigen Nachrichten an unbeteiligte Clients
- **Relevanz:** Clients erhalten nur Daten für ihren aktuellen Zeitraum

---

## Dateien

### Backend

| Datei | Zweck |
|-------|-------|
| `Infrastructure/Hubs/WorkNotificationHub.cs` | Hub-Klasse |
| `Infrastructure/Hubs/IWorkNotificationService.cs` | Interface für Work + PeriodHours |
| `Infrastructure/Hubs/WorkNotificationService.cs` | Implementation |
| `Infrastructure/Hubs/IShiftStatsNotificationService.cs` | Interface für ShiftStats |
| `Infrastructure/Hubs/ShiftStatsNotificationService.cs` | Implementation |
| `Presentation/DTOs/Notifications/WorkNotificationDto.cs` | DTO |
| `Presentation/DTOs/Notifications/ShiftStatsNotificationDto.cs` | DTO |
| `Presentation/DTOs/Notifications/PeriodHoursNotificationDto.cs` | DTO |

### Frontend

| Datei | Zweck |
|-------|-------|
| `infrastructure/signalr/signalr.service.ts` | SignalR-Client |
| `presentation/auth/auth.interceptor.ts` | ConnectionId Header |
| `domain/services/schedule/work-schedule-loader.service.ts` | PeriodHours Updates |
