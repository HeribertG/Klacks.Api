using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize]
public class WorkNotificationHub : Hub<IScheduleClient>
{
    private readonly ILogger<WorkNotificationHub> _logger;
    private readonly IConnectionDateRangeTracker _dateRangeTracker;

    public WorkNotificationHub(
        ILogger<WorkNotificationHub> logger,
        IConnectionDateRangeTracker dateRangeTracker)
    {
        _logger = logger;
        _dateRangeTracker = dateRangeTracker;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _dateRangeTracker.UnregisterConnection(Context.ConnectionId);
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    public async Task JoinScheduleGroup(string startDate, string endDate)
    {
        var groupName = GetScheduleGroupName(startDate, endDate);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        if (DateOnly.TryParse(startDate, out var start) && DateOnly.TryParse(endDate, out var end))
        {
            _dateRangeTracker.RegisterConnection(Context.ConnectionId, start, end);
            _logger.LogInformation(
                "SignalR GROUP JOIN: {ConnectionId} joined '{GroupName}', registered DateRange {Start} - {End}",
                Context.ConnectionId, groupName, start, end);
        }
        else
        {
            _logger.LogWarning(
                "SignalR GROUP JOIN: {ConnectionId} joined '{GroupName}', but could not parse dates",
                Context.ConnectionId, groupName);
        }
    }

    public async Task LeaveScheduleGroup(string startDate, string endDate)
    {
        var groupName = GetScheduleGroupName(startDate, endDate);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("SignalR GROUP LEAVE: {ConnectionId} left '{GroupName}'", Context.ConnectionId, groupName);
    }

    public async Task JoinClientGroup(string clientId)
    {
        var groupName = GetClientGroupName(clientId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("SignalR CLIENT GROUP JOIN: {ConnectionId} joined '{GroupName}'", Context.ConnectionId, groupName);
    }

    public async Task LeaveClientGroup(string clientId)
    {
        var groupName = GetClientGroupName(clientId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("SignalR CLIENT GROUP LEAVE: {ConnectionId} left '{GroupName}'", Context.ConnectionId, groupName);
    }

    public async Task JoinClientGroups(IEnumerable<string> clientIds)
    {
        var ids = clientIds?.ToList() ?? new List<string>();
        if (ids.Count == 0) return;

        // Process in batches to avoid blocking the connection
        const int batchSize = 50;
        for (int i = 0; i < ids.Count; i += batchSize)
        {
            var batch = ids.Skip(i).Take(batchSize);
            foreach (var clientId in batch)
            {
                var groupName = GetClientGroupName(clientId);
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            }
            // Yield to allow other operations between batches
            if (i + batchSize < ids.Count)
                await Task.Yield();
        }
        _logger.LogInformation("SignalR CLIENT GROUPS JOIN: {ConnectionId} joined {Count} client groups", Context.ConnectionId, ids.Count);
    }

    public async Task LeaveClientGroups(IEnumerable<string> clientIds)
    {
        var ids = clientIds?.ToList() ?? new List<string>();
        if (ids.Count == 0) return;

        // Process in batches to avoid blocking the connection
        const int batchSize = 50;
        for (int i = 0; i < ids.Count; i += batchSize)
        {
            var batch = ids.Skip(i).Take(batchSize);
            foreach (var clientId in batch)
            {
                var groupName = GetClientGroupName(clientId);
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            }
            // Yield to allow other operations between batches
            if (i + batchSize < ids.Count)
                await Task.Yield();
        }
        _logger.LogInformation("SignalR CLIENT GROUPS LEAVE: {ConnectionId} left {Count} client groups", Context.ConnectionId, ids.Count);
    }

    public static string GetScheduleGroupName(string startDate, string endDate)
        => SignalRConstants.Groups.Schedule(startDate, endDate);

    public static string GetScheduleGroupName(DateOnly startDate, DateOnly endDate)
        => SignalRConstants.Groups.Schedule(startDate, endDate);

    public static string GetClientGroupName(string clientId)
        => SignalRConstants.Groups.Client(clientId);

    public static string GetClientGroupName(Guid clientId)
        => SignalRConstants.Groups.Client(clientId);
}
