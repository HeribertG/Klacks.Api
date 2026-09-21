// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Detects active clients (membership valid today) lacking core data and emits ONE aggregated
/// ClientMissingCoreDataSummaryTriggerEvent PER MISSING FIELD naming everybody who lacks it: address
/// when no active address exists, contact when neither an e-mail nor a phone communication entry
/// exists. At most two events per tick instead of two per client.
/// Aggregated per field rather than into a single event because the two gaps carry different
/// severities and different long-standing sentences; merging them would need an invented severity and
/// would stop telling the planner what is actually missing.
/// Deliberately carries no result cap any more: the aggregates report a count, and a capped read would
/// report the cap instead of the truth. Both paths therefore run the identical uncapped repository
/// query - which is what the fingerprint scan alone already did before the aggregation.
/// </summary>
/// <param name="coreDataReadRepository">Read-only core-data quality scans.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="companyClock">Resolves the reference date as the company's own local day.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class ClientMissingCoreDataDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int UncappedResultCount = int.MaxValue;

    private const string UnknownMissingFieldMessage =
        "Unknown core-data field. AllMissingFields and Lacks have to be extended together: the previous "
        + "fallthrough reported every unknown field as a missing way to be contacted, so a new field "
        + "would have been announced under the wrong sentence and the wrong severity.";

    /// <summary>
    /// The emission order of the aggregates, most severe field first. One list shared by DetectAsync
    /// and the fingerprint scan so a new core-data field cannot reach one path without the other.
    /// </summary>
    private static readonly IReadOnlyList<string> AllMissingFields =
    [
        ClientMissingCoreDataTriggerEvent.AddressField,
        ClientMissingCoreDataTriggerEvent.ContactField
    ];

    private readonly IClientCoreDataReadRepository _coreDataReadRepository;
    private readonly ILogger<ClientMissingCoreDataDetector> _logger;
    private readonly ICompanyClock _companyClock;

    public ClientMissingCoreDataDetector(
        IClientCoreDataReadRepository coreDataReadRepository,
        ILogger<ClientMissingCoreDataDetector> logger,
        ICompanyClock companyClock)
    {
        _coreDataReadRepository = coreDataReadRepository;
        _logger = logger;
        _companyClock = companyClock;
    }

    public string Kind => AgentTriggerKinds.ClientMissingCoreData;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await ScanAsync(cancellationToken);
        if (statuses.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var events = new List<IAgentTriggerEvent>();
        foreach (var missingField in AllMissingFields)
        {
            var affected = AffectedBy(statuses, missingField);
            if (affected.Count == 0)
            {
                continue;
            }

            events.Add(new ClientMissingCoreDataSummaryTriggerEvent(affected, missingField));
        }

        _logger.LogInformation(
            "ClientMissingCoreData scan: {Clients} client(s) with gaps, {Events} aggregated event(s) emitted",
            statuses.Count, events.Count);

        return events;
    }

    /// <summary>
    /// Runs the identical scan for the identical reference date and folds it to one fingerprint per
    /// field that anybody is actually missing, so the set matches the events DetectAsync emits exactly.
    /// A field nobody is missing must NOT appear here, or its ledger row would stay open after the last
    /// gap was filled.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await ScanAsync(cancellationToken);

        return AllMissingFields
            .Where(missingField => AffectedBy(statuses, missingField).Count > 0)
            .Select(missingField => AgentConditionLedgerPolicy.FingerprintFor(
                Kind, ClientMissingCoreDataSummaryTriggerEvent.DedupKeyFor(missingField)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<List<ClientCoreDataStatus>> ScanAsync(CancellationToken cancellationToken) =>
        await _coreDataReadRepository.GetActiveClientsWithMissingCoreDataAsync(
            await _companyClock.GetTodayDateAsync(cancellationToken), UncappedResultCount, cancellationToken);

    private static List<ProactiveAffectedClient> AffectedBy(
        IReadOnlyList<ClientCoreDataStatus> statuses,
        string missingField) =>
        statuses
            .Where(status => Lacks(status, missingField))
            .Select(status => new ProactiveAffectedClient(status.ClientId, DisplayName(status)))
            .ToList();

    /// <summary>
    /// Whether this employee is missing the named field. Internal rather than private so the unknown-field
    /// branch can be tested: from inside this class it is unreachable by construction, because every call
    /// passes an entry of AllMissingFields.
    /// </summary>
    internal static bool Lacks(ClientCoreDataStatus status, string missingField) => missingField switch
    {
        ClientMissingCoreDataTriggerEvent.AddressField => !status.HasActiveAddress,
        ClientMissingCoreDataTriggerEvent.ContactField => !status.HasEmailOrPhone,
        _ => throw new ArgumentOutOfRangeException(
            nameof(missingField), missingField, UnknownMissingFieldMessage)
    };

    private static string DisplayName(ClientCoreDataStatus status)
    {
        var clientName = $"{status.FirstName} {status.Name}".Trim();

        return string.IsNullOrEmpty(clientName) ? status.ClientId.ToString() : clientName;
    }
}
