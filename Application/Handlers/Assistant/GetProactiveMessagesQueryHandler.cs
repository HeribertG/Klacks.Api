// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists a user's proactive inbox messages newest first, mapping each dispatch row to a DTO with
/// the i18n content key, deserialized content params (empty when absent or invalid), severity,
/// trigger kind, one-click action route and params (null when absent or invalid), reaction, read
/// state and whether the row reported a condition-ledger finding the "mach du" delegate action
/// (Etappe 4e) can act on. Normalizes the take parameter to the configured default and maximum.
/// </summary>
/// <param name="dispatchRepository">Persistence of the proactive trigger dispatch rows.</param>

using System.Text.Json;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetProactiveMessagesQueryHandler : IRequestHandler<GetProactiveMessagesQuery, IReadOnlyList<ProactiveInboxMessageDto>>
{
    private static readonly IReadOnlyDictionary<string, string> EmptyContentParams = new Dictionary<string, string>();

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;

    public GetProactiveMessagesQueryHandler(IProactiveTriggerDispatchRepository dispatchRepository)
    {
        _dispatchRepository = dispatchRepository;
    }

    public async Task<IReadOnlyList<ProactiveInboxMessageDto>> Handle(GetProactiveMessagesQuery request, CancellationToken cancellationToken)
    {
        var take = NormalizeTake(request.Take);
        var rows = await _dispatchRepository.ListForUserAsync(request.UserId, request.UnreadOnly, take, cancellationToken);
        return rows.Select(ToDto).ToList();
    }

    private static int NormalizeTake(int? take)
    {
        if (take is not int value || value <= 0)
        {
            return ProactiveInboxDefaults.DefaultListTake;
        }

        return Math.Min(value, ProactiveInboxDefaults.MaxListTake);
    }

    private static ProactiveInboxMessageDto ToDto(ProactiveTriggerDispatchRow row)
    {
        return new ProactiveInboxMessageDto
        {
            Id = row.Id,
            Content = row.ContentKey ?? string.Empty,
            ContentParams = DeserializeParams(row.ContentParamsJson) ?? EmptyContentParams,
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
