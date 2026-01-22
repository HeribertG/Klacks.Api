using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize]
public class WorkNotificationHub : Hub
{
    private readonly ILogger<WorkNotificationHub> _logger;

    public WorkNotificationHub(ILogger<WorkNotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
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
        _logger.LogDebug("Client {ConnectionId} joined group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveScheduleGroup(string startDate, string endDate)
    {
        var groupName = GetScheduleGroupName(startDate, endDate);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left group {GroupName}", Context.ConnectionId, groupName);
    }

    public static string GetScheduleGroupName(string startDate, string endDate)
    {
        return $"schedule_{startDate}_{endDate}";
    }

    public static string GetScheduleGroupName(DateOnly startDate, DateOnly endDate)
    {
        return $"schedule_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}";
    }
}
