# SignalR Documentation

**Letzte Aktualisierung:** 27.12.2025

---

## Übersicht

SignalR wird für Echtzeit-Benachrichtigungen zwischen Backend und Frontend verwendet.

---

## WorkNotificationHub

### Konfiguration

- **Pfad:** `/hubs/work-notifications`
- **Authentifizierung:** JWT Bearer (via `access_token` Query-Parameter)

### Events

| Event | Beschreibung | Payload |
|-------|--------------|---------|
| `WorkCreated` | Work wurde erstellt | `WorkResource` |
| `WorkUpdated` | Work wurde aktualisiert | `WorkResource` |
| `WorkDeleted` | Work wurde gelöscht | `{ workId: Guid }` |
| `ShiftStatsUpdated` | Shift-Statistiken aktualisiert | `ShiftStatsResource` |

### Header für Sender-Ausschluss

```
X-SignalR-ConnectionId: <connectionId>
```

Der Sender erhält keine eigene Notification. Das Frontend sendet die eigene ConnectionId im Header, damit das Backend den Sender von der Broadcast-Liste ausschließen kann.

---

## Frontend-Integration

### Verbindung herstellen

```typescript
import { HubConnectionBuilder, HubConnection } from '@microsoft/signalr';

const connection = new HubConnectionBuilder()
  .withUrl(`${environment.baseUrl}hubs/work-notifications`, {
    accessTokenFactory: () => this.authService.getToken()
  })
  .withAutomaticReconnect()
  .build();

await connection.start();
```

### Events abonnieren

```typescript
connection.on('WorkCreated', (work: WorkResource) => {
  console.log('Work created:', work);
});

connection.on('WorkUpdated', (work: WorkResource) => {
  console.log('Work updated:', work);
});

connection.on('WorkDeleted', (data: { workId: string }) => {
  console.log('Work deleted:', data.workId);
});

connection.on('ShiftStatsUpdated', (stats: ShiftStatsResource) => {
  console.log('Shift stats updated:', stats);
});
```

### ConnectionId für API-Calls

```typescript
const connectionId = connection.connectionId;

// Bei API-Calls mitsenden
this.http.post('/api/backend/Works', workData, {
  headers: { 'X-SignalR-ConnectionId': connectionId }
});
```

---

## Backend-Implementation

### Hub-Klasse

```csharp
[Authorize]
public class WorkNotificationHub : Hub
{
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

### Notification Service

```csharp
public class WorkNotificationService : IWorkNotificationService
{
    private readonly IHubContext<WorkNotificationHub> _hubContext;

    public async Task NotifyWorkCreated(WorkResource work, string? excludeConnectionId)
    {
        var clients = excludeConnectionId != null
            ? _hubContext.Clients.AllExcept(excludeConnectionId)
            : _hubContext.Clients.All;

        await clients.SendAsync("WorkCreated", work);
    }

    public async Task NotifyWorkUpdated(WorkResource work, string? excludeConnectionId)
    {
        var clients = excludeConnectionId != null
            ? _hubContext.Clients.AllExcept(excludeConnectionId)
            : _hubContext.Clients.All;

        await clients.SendAsync("WorkUpdated", work);
    }

    public async Task NotifyWorkDeleted(Guid workId, string? excludeConnectionId)
    {
        var clients = excludeConnectionId != null
            ? _hubContext.Clients.AllExcept(excludeConnectionId)
            : _hubContext.Clients.All;

        await clients.SendAsync("WorkDeleted", new { workId });
    }
}
```

### Registrierung in Program.cs

```csharp
builder.Services.AddSignalR();
builder.Services.AddScoped<IWorkNotificationService, WorkNotificationService>();

// ...

app.MapHub<WorkNotificationHub>("/hubs/work-notifications");
```

---

## Dateien

### Backend

| Datei | Zweck |
|-------|-------|
| `Presentation/Hubs/WorkNotificationHub.cs` | Hub-Klasse |
| `Application/Interfaces/IWorkNotificationService.cs` | Interface |
| `Infrastructure/Services/WorkNotificationService.cs` | Implementation |

### Frontend

| Datei | Zweck |
|-------|-------|
| `infrastructure/signalr/work-notification.service.ts` | SignalR-Client |
