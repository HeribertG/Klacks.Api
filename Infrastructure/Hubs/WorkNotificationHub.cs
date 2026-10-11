// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// SignalR hub for schedule change notifications. Joining a scenario's schedule group and choosing the group filter are
/// checked against the caller's visibility; the caller is the connection's own user, never an empty request context.
/// </summary>
/// <param name="scenarioTokenAccessGuard">Refuses analyse tokens of scenarios the caller may not see</param>
/// <param name="groupVisibilityGuard">Refuses a group filter on a group the caller may not see and resolves the caller's
/// visibility scope, which is cached on the connection state (ConnectionVisibility) when the connection joins a schedule
/// range or changes its group filter, so the notification sender filters without resolving visibility per message</param>
/// <param name="groupClients">Maps the caller's visible group trees to their clients for that cache</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Security.Claims;

namespace Klacks.Api.Infrastructure.Hubs;

[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class WorkNotificationHub : Hub<IScheduleClient>
{
    private readonly ILogger<WorkNotificationHub> _logger;
    private readonly IConnectionDateRangeTracker _dateRangeTracker;
    private readonly IScheduleTimelineService _timelineService;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClients;

    public WorkNotificationHub(
        ILogger<WorkNotificationHub> logger,
        IConnectionDateRangeTracker dateRangeTracker,
        IScheduleTimelineService timelineService,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        IGroupVisibilityGuard groupVisibilityGuard,
        IGetAllClientIdsFromGroupAndSubgroups groupClients)
    {
        _logger = logger;
        _dateRangeTracker = dateRangeTracker;
        _timelineService = timelineService;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
        _groupVisibilityGuard = groupVisibilityGuard;
        _groupClients = groupClients;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _dateRangeTracker.UnregisterConnectionAsync(Context.ConnectionId);
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public string GetConnectionId()
    {
        return Context.ConnectionId;
    }

    public async Task JoinScheduleGroup(string startDate, string endDate, string? analyseToken)
    {
        using var caller = BeginCallerPrincipal();
        var token = ParseAnalyseToken(analyseToken);
        if (caller is null || !await _scenarioTokenAccessGuard.IsVisibleAsync(token, Context.ConnectionAborted))
        {
            _logger.LogWarning(
                "SignalR GROUP JOIN refused: {ConnectionId} asked for a scenario it may not see", Context.ConnectionId);
            return;
        }

        await _dateRangeTracker.SetVisibilityAsync(Context.ConnectionId, await ResolveVisibilityAsync(), Context.ConnectionAborted);

        var groupName = GetScheduleGroupName(startDate, endDate, analyseToken);

        var previous = await _dateRangeTracker.GetConnectionAsync(Context.ConnectionId);
        var previousToken = previous?.AnalyseToken;

        if (previous is not null && previousToken != token)
        {
            var previousGroupName = SignalRConstants.Groups.Schedule(previous.Start, previous.End, previousToken);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, previousGroupName);
            _logger.LogDebug(
                "[COLLISION-TRACE] JoinScheduleGroup: {ConnectionId} left previous group '{PrevGroupName}' (token changed {PrevToken} -> {NewToken})",
                Context.ConnectionId, previousGroupName, previousToken?.ToString() ?? "null", token?.ToString() ?? "null");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        if (DateOnly.TryParse(startDate, CultureInfo.InvariantCulture, out var start)
            && DateOnly.TryParse(endDate, CultureInfo.InvariantCulture, out var end))
        {
            await _dateRangeTracker.RegisterConnectionAsync(Context.ConnectionId, start, end, token);

            var dateRangeChanged = previous is null || previous.Start != start || previous.End != end;
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

    public async Task SetSelectedGroup(string selectedGroupId)
    {
        using var caller = BeginCallerPrincipal();
        Guid? parsedGroupId = null;
        if (!string.IsNullOrEmpty(selectedGroupId) && Guid.TryParse(selectedGroupId, out var gid) && gid != Guid.Empty)
        {
            parsedGroupId = gid;
        }

        if (caller is null
            || (parsedGroupId is { } groupId && !await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, Context.ConnectionAborted)))
        {
            _logger.LogWarning(
                "SignalR SetSelectedGroup refused: {ConnectionId} asked for a group it may not see", Context.ConnectionId);
            return;
        }

        var previous = await _dateRangeTracker.GetConnectionAsync(Context.ConnectionId);
        await _dateRangeTracker.SetSelectedGroupAsync(Context.ConnectionId, parsedGroupId);
        await _dateRangeTracker.SetVisibilityAsync(Context.ConnectionId, await ResolveVisibilityAsync(), Context.ConnectionAborted);

        var groupChanged = previous?.SelectedGroupId != parsedGroupId;
        if (groupChanged && previous is not null)
        {
            _timelineService.QueueRangeCheck(previous.Start, previous.End, previous.AnalyseToken);
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
        using var caller = BeginCallerPrincipal();
        if (caller is null || !IsVisibleClient(await CurrentVisibilityAsync(), clientId))
        {
            _logger.LogWarning(
                "SignalR CLIENT GROUP JOIN refused: {ConnectionId} asked for a client it may not see", Context.ConnectionId);
            return;
        }

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
        using var caller = BeginCallerPrincipal();
        if (caller is null) return;

        var requested = clientIds?.ToList() ?? new List<string>();
        if (requested.Count == 0) return;

        var visibility = await CurrentVisibilityAsync();
        var ids = requested.Where(clientId => IsVisibleClient(visibility, clientId)).ToList();
        if (ids.Count < requested.Count)
        {
            _logger.LogWarning(
                "SignalR CLIENT GROUPS JOIN: {ConnectionId} skipped {Count} clients it may not see",
                Context.ConnectionId, requested.Count - ids.Count);
        }

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

    /// <summary>
    /// Runs the rest of the invocation on behalf of the connection's own user (NameIdentifier of Context.User), so the
    /// visibility guards see that user even when IHttpContextAccessor has no request inside the hub invocation (long
    /// polling); without it they would treat the call as an unrestricted background job. Null when the connection
    /// carries no valid user id - the caller must then refuse.
    /// </summary>
    private IDisposable? BeginCallerPrincipal()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userId, out var callerId) || callerId == Guid.Empty)
        {
            _logger.LogWarning("SignalR call refused: {ConnectionId} carries no user id", Context.ConnectionId);
            return null;
        }

        return ExecutionPrincipal.Begin(callerId);
    }

    /// <summary>
    /// The caller's visibility: unrestricted, or the clients and groups of the caller's visible group trees. Must run
    /// inside <see cref="BeginCallerPrincipal"/>.
    /// </summary>
    private async Task<ConnectionVisibility> ResolveVisibilityAsync()
    {
        var scope = await _groupVisibilityGuard.GetScopeAsync(Context.ConnectionAborted);
        if (scope.IsUnrestricted)
        {
            return ConnectionVisibility.Unrestricted;
        }

        var clientIds = scope.VisibleRootIds.Count == 0
            ? []
            : await _groupClients.GetAllClientIdsFromGroupsAndSubgroupsFromList(scope.VisibleRootIds.ToList());
        return ConnectionVisibility.Restricted(clientIds, scope.VisibleGroupIds);
    }

    /// <summary>
    /// The visibility cached on the connection state, or a fresh resolution when the connection has none yet.
    /// </summary>
    private async Task<ConnectionVisibility> CurrentVisibilityAsync()
    {
        var connection = await _dateRangeTracker.GetConnectionAsync(Context.ConnectionId, Context.ConnectionAborted);
        return connection is not null && !ReferenceEquals(connection.Visibility, ConnectionVisibility.None)
            ? connection.Visibility
            : await ResolveVisibilityAsync();
    }

    private static bool IsVisibleClient(ConnectionVisibility visibility, string clientId)
        => Guid.TryParse(clientId, out var id) && visibility.AllowsClient(id);

    private static Guid? ParseAnalyseToken(string? analyseToken)
    {
        if (string.IsNullOrWhiteSpace(analyseToken)) return null;
        return Guid.TryParse(analyseToken, out var g) ? g : (Guid?)null;
    }
}
