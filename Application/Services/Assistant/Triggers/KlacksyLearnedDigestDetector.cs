// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Emits the weekly "Klacksy learned something" digest. Runs on the shared hourly detector tick rather
/// than on a clock of its own: the weekly rhythm is date logic here plus an ISO-week dedup key, which is
/// what keeps the pipeline at exactly one timer. The reported window is the PREVIOUS ISO week, not the
/// running one - on Monday morning the running week is empty, so a digest over it would always report
/// nothing and never fire. Reporting the finished week also makes a late first tick a catch-up rather
/// than a loss: any tick within the week emits the same event under the same dedup key.
/// Implements IAgentConditionFingerprintSource so the condition ledger resolves last week's row on its
/// own once the week is over, instead of keeping it open forever.
/// </summary>
/// <param name="clusterRepository">Counts which clusters entered a reportable status in the window</param>
/// <param name="proposalRepository">Counts the description sharpenings the regression gate withheld</param>
/// <param name="companyClock">Supplies the operator's local date, so the week boundary is theirs, not the server's</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed class KlacksyLearnedDigestDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const int DaysPerWeek = 7;

    // Ready is deliberately absent. Before the learning loop existed it was the only status a wish could
    // reach, so counting it was the difference between a digest and silence; now the loop drains it within
    // hours, and reporting it would count wishes that are mid-flight as wishes nobody can serve.
    private static readonly IReadOnlyList<string> ReportedStatuses =
    [
        SkillLearningClusterStatuses.LearnedPhrase,
        SkillLearningClusterStatuses.LearnedCapability,
        SkillLearningClusterStatuses.Unfulfillable
    ];

    private static readonly IReadOnlyList<string> BlockedStatuses = [ProposedChangeStatuses.BlockedRegression];

    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly ICompanyClock _companyClock;

    public KlacksyLearnedDigestDetector(
        ISkillLearningClusterRepository clusterRepository,
        IProposedSkillChangeRepository proposalRepository,
        ICompanyClock companyClock)
    {
        _clusterRepository = clusterRepository;
        _proposalRepository = proposalRepository;
        _companyClock = companyClock;
    }

    public string Kind => AgentTriggerKinds.KlacksyLearnedDigest;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var digest = await BuildDigestAsync(cancellationToken);
        return digest == null ? [] : [digest];
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var digest = await BuildDigestAsync(cancellationToken);

        return digest == null
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(
                [AgentConditionLedgerPolicy.FingerprintFor(Kind, digest.DedupKey)],
                StringComparer.Ordinal);
    }

    private async Task<KlacksyLearnedDigestTriggerEvent?> BuildDigestAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(await _companyClock.GetTodayAsync(cancellationToken));
        var currentWeekStart = StartOfIsoWeek(today);
        var reportedWeekStart = currentWeekStart.AddDays(-DaysPerWeek);

        var fromUtc = reportedWeekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = currentWeekStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var counts = await _clusterRepository.CountByStatusInWindowAsync(
            ReportedStatuses, fromUtc, toUtc, cancellationToken);

        var proposalCounts = await _proposalRepository.CountByStatusInWindowAsync(
            BlockedStatuses, fromUtc, toUtc, cancellationToken);

        var phrases = Count(counts, SkillLearningClusterStatuses.LearnedPhrase);
        var capabilities = Count(counts, SkillLearningClusterStatuses.LearnedCapability);
        var unfulfillable = Count(counts, SkillLearningClusterStatuses.Unfulfillable);
        var blocked = Count(proposalCounts, ProposedChangeStatuses.BlockedRegression);

        if (phrases + capabilities + unfulfillable + blocked == 0)
        {
            return null;
        }

        return new KlacksyLearnedDigestTriggerEvent(
            reportedWeekStart, phrases, capabilities, unfulfillable, blocked);
    }

    private static int Count(IReadOnlyDictionary<string, int> counts, string status) =>
        counts.TryGetValue(status, out var count) ? count : 0;

    private static DateOnly StartOfIsoWeek(DateOnly date)
    {
        var isoDayOfWeek = date.DayOfWeek == DayOfWeek.Sunday
            ? DaysPerWeek
            : (int)date.DayOfWeek;

        return date.AddDays(1 - isoDayOfWeek);
    }
}
