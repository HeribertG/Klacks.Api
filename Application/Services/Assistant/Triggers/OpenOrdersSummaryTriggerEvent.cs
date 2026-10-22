// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// ONE daily message per planner about the orders that are still open: "N open orders, X of them start within
/// the next seven days". Replaces the former one-message-per-order shape (OpenOrderTriggerEvent), which put
/// hundreds of near-identical rows into every planner's inbox. Emitted by OpenOrderDetector at most once per
/// tick, carrying every open order of the installation; AgentTriggerService narrows it per recipient
/// (IGroupScopedAggregateTriggerEvent), so N and X are counted over exactly the orders that recipient may see,
/// and orders without any group count for Admins only.
///
/// The dedup key is the company day: one ledger row per day. Per recipient the event is an IRisingCountTriggerEvent:
/// the first summary of the day is delivered, and a further one only when that recipient's count has risen above
/// the highest count they already received that day (new orders after the morning message, typically right after
/// an ERP import); the same or a lower count sends nothing (owner decision 2026-10-22). The kind stays open_order, so the per-order rows of the old shape leave the active fingerprint set on the first
/// tick and are resolved by reconcile without any message. The payload deliberately carries no count: the inbox
/// read and the reminder sweep merge the live ledger payload over every recipient's frozen parameters
/// (ProactiveContentParamMerge), and an installation-wide count would overwrite a planner's narrowed one.
///
/// Severity is High only for a recipient's own variant (IsRecipientView) with an order starting within the soon
/// window; the unnarrowed event - the one the condition ledger stores - is never more than Medium. The ledger row is
/// shared by every planner who may see any of its groups, and AgentConditionDigestService counts its severity for
/// all of them; a High there would let another group's orders make a planner's daily digest High, which is allowed
/// to wake that planner over the messenger at night.
/// </summary>
/// <param name="CompanyDay">The company's own "today" the scan ran on; dedup anchor and start of the soon window</param>
/// <param name="Orders">The open orders this variant counts; the unnarrowed event holds all of them</param>
/// <param name="IsRecipientView">True for a variant narrowed to one recipient's view; only such a variant may be High</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record OpenOrdersSummaryTriggerEvent(
    DateOnly CompanyDay,
    IReadOnlyList<OpenOrderSnapshot> Orders,
    bool IsRecipientView = false) : IGroupScopedAggregateTriggerEvent, IRisingCountTriggerEvent
{
    public const int SoonWindowDays = 7;

    private const string CountParam = "count";
    private const string SoonCountParam = "soonCount";
    private const string CompanyDayPayloadKey = "companyDay";

    public string Kind => AgentTriggerKinds.OpenOrder;

    public int Count => Orders.Count;

    /// <summary>
    /// Orders whose first day lies between the company day and the company day plus SoonWindowDays, both
    /// inclusive. An order that started before today and is still running is counted in Count only.
    /// </summary>
    public int SoonCount => Orders.Count(order => IsSoon(order.FromDate));

    public string Severity => IsRecipientView && SoonCount > 0 ? AgentTriggerSeverity.High : AgentTriggerSeverity.Medium;

    public int RisingCount => Count;

    public bool PlannersOnly => true;

    public bool RequiresGroupScope => true;

    public IReadOnlyCollection<Guid> GroupIds => Orders
        .SelectMany(order => order.GroupIds)
        .Distinct()
        .ToArray();

    public Guid? EntityId => null;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.OpenOrdersSummary;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        [CountParam] = Count.ToString(CultureInfo.InvariantCulture),
        [SoonCountParam] = SoonCount.ToString(CultureInfo.InvariantCulture)
    };

    public string DedupKey => DedupKeyFor(CompanyDay);

    /// <summary>
    /// The DedupKey spelling as a function of the company day, so the detector's fingerprint scan builds the
    /// identical key without restating the format.
    /// </summary>
    /// <param name="companyDay">The company day the summary is about</param>
    public static string DedupKeyFor(DateOnly companyDay) =>
        companyDay.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams => new Dictionary<string, string>
    {
        [ProactiveActionParamKeys.Date] = FirstUpcomingDay().ToString(ProactiveMessageFormats.ActionDate, CultureInfo.InvariantCulture)
    };

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        [CompanyDayPayloadKey] = CompanyDay
    };

    public IAgentTriggerEvent? NarrowTo(IReadOnlySet<Guid> visibleGroupIds, bool includesUngrouped)
    {
        var visibleOrders = Orders
            .Where(order => order.GroupIds.Count == 0
                ? includesUngrouped
                : order.GroupIds.Any(visibleGroupIds.Contains))
            .ToList();

        return visibleOrders.Count == 0 ? null : this with { Orders = visibleOrders, IsRecipientView = true };
    }

    private bool IsSoon(DateOnly fromDate) =>
        fromDate >= CompanyDay && fromDate <= CompanyDay.AddDays(SoonWindowDays);

    private DateOnly FirstUpcomingDay()
    {
        var upcoming = Orders.Where(order => order.FromDate >= CompanyDay).Select(order => order.FromDate).ToList();
        return upcoming.Count == 0 ? CompanyDay : upcoming.Min();
    }
}
