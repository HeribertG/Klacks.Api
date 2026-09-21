// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists a user's proactive inbox messages newest first, mapping each dispatch row to a DTO with
/// the i18n content key, deserialized content params (empty when absent or invalid), severity,
/// trigger kind, one-click action route and params (null when absent or invalid), reaction, read
/// state and whether the row reported a condition-ledger finding the "mach du" delegate action
/// (Etappe 4e) can act on. Normalizes the take parameter to the configured default and maximum.
///
/// A row that reports a STILL-OPEN ledger row has its content params re-rendered from that row's current
/// payload (ProactiveContentParamMerge, the same merge the reminder sweep delivers with) instead of from
/// the copy frozen onto the dispatch row at first delivery. The aggregated findings state a count and a
/// name list in their sentence and their dedup key deliberately carries no date, so the dispatch row is
/// written exactly once and its frozen params report the count of the day the gap was first detected -
/// twelve missing addresses stayed twelve after forty people were affected. The dedup key is untouched by
/// this: the cadence of the notification stays "once per gap until it is filled", only the number the
/// sentence states follows the finding. Closed conditions and rows without one keep their frozen params,
/// because a finished finding's last known numbers are what it is a record of.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>
/// <param name="conditionRepository">Resolves the ledger rows the listed dispatch rows report, batched into one read.</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetProactiveMessagesQueryHandler : IRequestHandler<GetProactiveMessagesQuery, IReadOnlyList<ProactiveInboxMessageDto>>
{
    private static readonly IReadOnlyDictionary<string, string> EmptyContentParams = new Dictionary<string, string>();

    private static readonly IReadOnlyDictionary<Guid, string> NoLivePayloads = new Dictionary<Guid, string>();

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentConditionRepository _conditionRepository;

    public GetProactiveMessagesQueryHandler(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentConditionRepository conditionRepository)
    {
        _dispatchRepository = dispatchRepository;
        _conditionRepository = conditionRepository;
    }

    public async Task<IReadOnlyList<ProactiveInboxMessageDto>> Handle(GetProactiveMessagesQuery request, CancellationToken cancellationToken)
    {
        var take = NormalizeTake(request.Take);
        var rows = await _dispatchRepository.ListForUserAsync(request.UserId, request.UnreadOnly, take, cancellationToken);
        var livePayloads = await LoadLivePayloadsAsync(rows, cancellationToken);

        return rows.Select(row => ToDto(row, livePayloads)).ToList();
    }

    private static int NormalizeTake(int? take)
    {
        if (take is not int value || value <= 0)
        {
            return ProactiveInboxDefaults.DefaultListTake;
        }

        return Math.Min(value, ProactiveInboxDefaults.MaxListTake);
    }

    /// <summary>
    /// The current payload per still-open ledger row reported by this page of dispatch rows. ONE read for
    /// the whole page, not one per row: the inbox list is the most frequently polled assistant read there
    /// is, and a per-row lookup would turn every refresh into ProactiveInboxDefaults.MaxListTake queries.
    /// Rows whose condition is closed or missing are left out of the result, which is what makes them fall
    /// back to their frozen params in <see cref="ResolveContentParams"/>.
    /// </summary>
    /// <param name="rows">The dispatch rows about to be mapped.</param>
    /// <param name="cancellationToken">Cancels the ledger read.</param>
    private async Task<IReadOnlyDictionary<Guid, string>> LoadLivePayloadsAsync(
        IReadOnlyList<ProactiveTriggerDispatchRow> rows,
        CancellationToken cancellationToken)
    {
        var conditionIds = rows
            .Where(row => row.ConditionId.HasValue)
            .Select(row => row.ConditionId!.Value)
            .Distinct()
            .ToList();

        if (conditionIds.Count == 0)
        {
            return NoLivePayloads;
        }

        var conditions = await _conditionRepository.GetByIdsAsync(conditionIds, cancellationToken);
        var livePayloads = new Dictionary<Guid, string>();

        foreach (var condition in conditions)
        {
            if (AgentConditionStateMachine.IsOpen(condition.Status))
            {
                livePayloads[condition.Id] = condition.PayloadJson;
            }
        }

        return livePayloads;
    }

    private static ProactiveInboxMessageDto ToDto(
        ProactiveTriggerDispatchRow row,
        IReadOnlyDictionary<Guid, string> livePayloads)
    {
        return new ProactiveInboxMessageDto
        {
            Id = row.Id,
            Content = row.ContentKey ?? string.Empty,
            ContentParams = ResolveContentParams(row, livePayloads) ?? EmptyContentParams,
            Severity = row.Severity ?? string.Empty,
            Kind = row.TriggerKind,
            ActionRoute = row.ActionRoute,
            ActionParams = DeserializeParams(row.ActionParamsJson),
            Reaction = row.Reaction.ToString(),
            CreatedUtc = row.CreateTime,
            ReadAtUtc = row.ReadAtUtc,
            CanDelegate = row.ConditionId.HasValue,
            ReminderCount = row.ReminderCount,
            LastRemindedAtUtc = row.LastRemindedAtUtc,
            AcknowledgedAtUtc = row.AcknowledgedAtUtc
        };
    }

    /// <summary>
    /// The frozen params of the row, with the live payload of its open ledger row merged over them. A
    /// payload that does not parse degrades to the frozen params inside the merge; no log here, unlike in
    /// the reminder sweep, because this runs per polled request and a broken payload would fill the log
    /// with one line per refresh - the sweep already reports it once per reminder.
    /// </summary>
    /// <param name="row">The dispatch row being mapped.</param>
    /// <param name="livePayloads">Current payloads of the open ledger rows of this page, keyed by condition id.</param>
    private static IReadOnlyDictionary<string, string>? ResolveContentParams(
        ProactiveTriggerDispatchRow row,
        IReadOnlyDictionary<Guid, string> livePayloads)
    {
        var frozenParams = DeserializeParams(row.ContentParamsJson);

        if (row.ConditionId is not Guid conditionId
            || !livePayloads.TryGetValue(conditionId, out var livePayloadJson))
        {
            return frozenParams;
        }

        return ProactiveContentParamMerge.MergeLiveOverFrozen(frozenParams, livePayloadJson, out _);
    }

    private static IReadOnlyDictionary<string, string>? DeserializeParams(string? paramsJson)
    {
        if (string.IsNullOrWhiteSpace(paramsJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(paramsJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
