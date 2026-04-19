// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize]
public class WorkNotificationHub : Hub<IScheduleClient>
{
    private readonly ILogger<WorkNotificationHub> _logger;
    private readonly IConnectionDateRangeTracker _dateRangeTracker;
    private readonly IScheduleTimelineService _timelineService;

    public WorkNotificationHub(
        ILogger<WorkNotificationHub> logger,
        IConnectionDateRangeTracker dateRangeTracker,
        IScheduleTimelineService timelineService)
    {
        _logger = logger;
        _dateRangeTracker = dateRangeTracker;
        _timelineService = timelineService;
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

    public async Task JoinScheduleGroup(string startDate, string endDate, string? analyseToken)
    {
        var token = ParseAnalyseToken(analyseToken);
        var groupName = GetScheduleGroupName(startDate, endDate, analyseToken);

        var previousRange = _dateRangeTracker.GetRegisteredDateRange(Context.ConnectionId);
        var previousToken = _dateRangeTracker.GetAnalyseToken(Context.ConnectionId);

        if (previousRange.HasValue && previousToken != token)
        {
            var previousGroupName = SignalRConstants.Groups.Schedule(previousRange.Value.Start, previousRange.Value.End, previousToken);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousGroupName);
            _logger.LogDebug(
                "[COLLISION-TRACE] JoinScheduleGroup: {ConnectionId} left previous group '{PrevGroupName}' (token changed {PrevToken} -> {NewToken})",
                Context.ConnectionId, previousGroupName, previousToken?.ToString() ?? "null", token?.ToString() ?? "null");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        if (DateOnly.TryParse(startDate, out var start) && DateOnly.TryParse(endDate, out var end))
        {
            _dateRangeTracker.RegisterConnection(Context.ConnectionId, start, end, token);

            var dateRangeChanged = previousRange == null || previousRange.Value.Start != start || previousRange.Value.End != end;
            var tokenChanged = previousToken != token;

            if (dateRangeChanged || tokenChanged)
            {
                _timelineService.QueueRangeCheck(start, end, token);
                _logger.LogDebug(
                    "[COLLISION-TRACE] JoinScheduleGroup: {ConnectionId} joined '{GroupName}', DateRange {Start} - {End} token={Token}, QueueRangeCheck triggered (dateRangeChanged={DateRangeChanged} tokenChanged={TokenChanged})",
                    Context.ConnectionId, groupName, start, end, token?.ToString() ?? "null", dateRangeChanged, tokenChanged);
            }
            else
            {
                _logger.LogDebug(
                    "[COLLISION-TRACE] JoinScheduleGroup: {ConnectionId} joined '{GroupName}', DateRange {Start} - {End} token={Token}, SKIPPED QueueRangeCheck (same dateRange and token)",
                    Context.ConnectionId, groupName, start, end, token?.ToString() ?? "null");
            }
        }
        else
        {
            _logger.LogWarning(
                "SignalR GROUP JOIN: {ConnectionId} joined '{GroupName}', but could not parse dates",
                Context.ConnectionId, groupName);
        }
    }

    public void SetSelectedGroup(string selectedGroupId)
    {
        Guid? parsedGroupId = null;
        if (!string.IsNullOrEmpty(selectedGroupId) && Guid.TryParse(selectedGroupId, out var gid) && gid != Guid.Empty)
        {
            parsedGroupId = gid;
        }

        var previousGroup = _dateRangeTracker.GetSelectedGroup(Context.ConnectionId);
        _dateRangeTracker.SetSelectedGroup(Context.ConnectionId, parsedGroupId);

        var groupChanged = previousGroup != parsedGroupId;
        if (groupChanged)
        {
            var dateRange = _dateRangeTracker.GetRegisteredDateRange(Context.ConnectionId);
            if (dateRange.HasValue)
            {
                var token = _dateRangeTracker.GetAnalyseToken(Context.ConnectionId);
                _timelineService.QueueRangeCheck(dateRange.Value.Start, dateRange.Value.End, token);
            }
        }

        _logger.LogDebug(
            "[COLLISION-TRACE] SetSelectedGroup: {ConnectionId} selectedGroup={GroupId} groupChanged={Changed}",
            Context.ConnectionId, parsedGroupId?.ToString() ?? "(alle)", groupChanged);
    }

    public async Task LeaveScheduleGroup(string startDate, string endDate, string? analyseToken)
    {
        var groupName = GetScheduleGroupName(startDate, endDate, analyseToken);
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

    public static string GetScheduleGroupName(string startDate, string endDate, string? analyseToken)
        => SignalRConstants.Groups.Schedule(startDate, endDate, analyseToken);

    public static string GetScheduleGroupName(DateOnly startDate, DateOnly endDate, Guid? analyseToken)
        => SignalRConstants.Groups.Schedule(startDate, endDate, analyseToken);

    public static string GetClientGroupName(string clientId)
        => SignalRConstants.Groups.Client(clientId);

    public static string GetClientGroupName(Guid clientId)
        => SignalRConstants.Groups.Client(clientId);

    private static Guid? ParseAnalyseToken(string? analyseToken)
    {
        if (string.IsNullOrWhiteSpace(analyseToken)) return null;
        return Guid.TryParse(analyseToken, out var g) ? g : (Guid?)null;
    }
}
