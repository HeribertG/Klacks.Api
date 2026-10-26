// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Searches shift definitions by an optional search string and returns a compact list
/// (id, name, abbreviation, validity dates, start/end times, client name). Thin wrapper
/// around <see cref="Klacks.Api.Application.Queries.Shifts.GetTruncatedListQuery"/> that
/// covers active, former and future shifts. The list view is chosen with the status parameter;
/// the default is the plannable shifts (the rows place_work, cut_shift, update_shift and delete_shift
/// work on), because the query's own default is the unsealed order inbox, which holds none of them.
/// When the database text filter finds nothing, a fuzzy fallback ranks the spoken or decorated
/// query ("Dienst All-Shift") against the real shift names and abbreviations in memory and suggests
/// the closest shifts. Use this to find a shiftId for get_shift_details / update_shift / delete_shift.
/// The fallback resolves with the Strict policy although the skill only reads: recipes capture the
/// lone returned shift id and inject it into a write step, and the model reuses it for update/delete.
/// A shift that only a typo or phonetic stage matched is therefore returned as an ask-back text with
/// no rows, so no capture can pick it up before the user confirmed the name.
/// Optional location filters (country, state, city, zip prefix) narrow the list to orders by their location: the
/// customer's workplace address, else the main address, never the invoicing address (OrderLocationAddressRule); it has
/// nothing to do with the group an order is linked to. A location search skips the fuzzy name fallback, which would
/// suggest shifts from anywhere, and the rows never carry the address itself.
/// </summary>
/// <param name="searchString">Optional. Text filter on shift name, abbreviation or client name;
/// falls back to fuzzy name resolution when the filter finds nothing.</param>
/// <param name="maxResults">Optional. Maximum number of shifts to return (default 20, capped at 100).</param>
/// <param name="status">Optional. List view: shift (default, plannable shifts and cuts), draft_order (unsealed
/// orders), sealed_order (sealed orders) or container.</param>
/// <param name="country">Optional. Country of the order location (code or name in any language).</param>
/// <param name="state">Optional. State of the order location - canton, Bundesland, département, ... (code or name).</param>
/// <param name="city">Optional. City of the order location (case-insensitive equality).</param>
/// <param name="zipPrefix">Optional. Prefix of the order location's zip code.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("search_shifts")]
public class SearchShiftsSkill : BaseSkillImplementation
{
    private const int DefaultMaxResults = 20;
    private const int MaxAllowedResults = 100;
    private const int FuzzyCatalogCap = 500;
    private const int NotFoundNameSampleSize = 20;

    private const string StatusParameter = "status";
    private const string StatusShift = "shift";
    private const string StatusDraftOrder = "draft_order";
    private const string StatusSealedOrder = "sealed_order";
    private const string StatusContainer = "container";

    private const string CountryParameter = "country";
    private const string StateParameter = "state";
    private const string CityParameter = "city";
    private const string ZipPrefixParameter = "zipPrefix";

    private static readonly string[] ShiftLabelWords = ["dienst", "schicht", "shift", "service", "turno"];

    private static readonly Dictionary<string, (ShiftFilterType FilterType, bool IsSealedOrder)> Views =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [StatusShift] = (ShiftFilterType.Shift, false),
            [StatusDraftOrder] = (ShiftFilterType.Original, false),
            [StatusSealedOrder] = (ShiftFilterType.Original, true),
            [StatusContainer] = (ShiftFilterType.Container, false)
        };

    private readonly IMediator _mediator;

    public SearchShiftsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchString = (GetParameter<string>(parameters, "searchString") ?? string.Empty).Trim();
        var maxResults = GetParameter<int?>(parameters, "maxResults") ?? DefaultMaxResults;

        if (maxResults <= 0)
        {
            return SkillResult.Error("Parameter 'maxResults' must be greater than zero.");
        }

        if (maxResults > MaxAllowedResults)
        {
            maxResults = MaxAllowedResults;
        }

        var status = (GetParameter<string>(parameters, StatusParameter) ?? StatusShift).Trim();
        if (!Views.TryGetValue(status, out var view))
        {
            return SkillResult.Error(
                $"Parameter '{StatusParameter}' must be one of: {string.Join(", ", Views.Keys)}.");
        }

        var location = ReadLocation(parameters);
        var filter = BuildFilter(searchString, maxResults, view);
        ApplyLocation(filter, location);

        var result = await _mediator.Send(new GetTruncatedListQuery(filter), cancellationToken);

        var totalCount = result.MaxItems;
        var matched = result.Shifts ?? new List<ShiftResource>();
        var message = $"Found {totalCount} shift(s)" +
                      (!string.IsNullOrWhiteSpace(searchString) ? $" matching '{searchString}'" : string.Empty) +
                      DescribeLocation(location) +
                      $"; returning {matched.Count}." +
                      (totalCount == 0 && filter.HasLocationFilter ? EmptyLocationHint(status) : string.Empty);

        // A spoken or decorated name ("Dienst All-Shift") rarely survives the database text
        // filter — rank it against the real shift names instead of reporting an empty list.
        if (totalCount == 0 && !string.IsNullOrWhiteSpace(searchString) && !filter.HasLocationFilter)
        {
            (matched, message) = await ResolveFuzzyAsync(searchString, maxResults, view, cancellationToken);
            totalCount = matched.Count;
        }

        var shifts = matched
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Abbreviation,
                Status = s.Status.ToString(),
                s.OriginalId,
                s.FromDate,
                s.UntilDate,
                s.StartShift,
                s.EndShift,
                ClientName = FormatClientName(s.Client)
            })
            .ToList();

        return SkillResult.SuccessResult(new { TotalCount = totalCount, Shifts = shifts }, message);
    }

    private static (string? Country, string? State, string? City, string? ZipPrefix) ReadLocation(
        Dictionary<string, object> parameters)
        => (Clean(GetParameter<string>(parameters, CountryParameter)),
            Clean(GetParameter<string>(parameters, StateParameter)),
            Clean(GetParameter<string>(parameters, CityParameter)),
            Clean(GetParameter<string>(parameters, ZipPrefixParameter)));

    private static void ApplyLocation(
        ShiftFilter filter, (string? Country, string? State, string? City, string? ZipPrefix) location)
    {
        filter.Country = location.Country;
        filter.State = location.State;
        filter.City = location.City;
        filter.ZipPrefix = location.ZipPrefix;
    }

    private static string DescribeLocation((string? Country, string? State, string? City, string? ZipPrefix) location)
    {
        string?[] candidates =
        [
            location.City == null ? null : $"city '{location.City}'",
            location.ZipPrefix == null ? null : $"zip starting '{location.ZipPrefix}'",
            location.State == null ? null : $"state '{location.State}'",
            location.Country == null ? null : $"country '{location.Country}'"
        ];
        var parts = candidates.OfType<string>().ToList();

        return parts.Count == 0
            ? string.Empty
            : " whose customer location (workplace, else main address) is in " + string.Join(", ", parts);
    }

    private static string EmptyLocationHint(string status)
        => " No customer address matches there in this list. A city and a state can share a name (Bern): retry with the " +
           "other parameter if the user may have meant it" +
           (string.Equals(status, StatusShift, StringComparison.OrdinalIgnoreCase)
               ? $", and for orders use {StatusParameter}={StatusDraftOrder} or {StatusSealedOrder}"
               : string.Empty) +
           ". Never say there are no orders in a place before both were checked.";

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ShiftFilter BuildFilter(
        string searchString, int pageSize, (ShiftFilterType FilterType, bool IsSealedOrder) view)
    {
        return new ShiftFilter
        {
            SearchString = searchString,
            NumberOfItemsPerPage = pageSize,
            ActiveDateRange = true,
            FormerDateRange = true,
            FutureDateRange = true,
            IncludeClientName = true,
            FilterType = view.FilterType,
            IsSealedOrder = view.IsSealedOrder,
            IsTimeRange = true,
            IsSporadic = true
        };
    }

    private async Task<(List<ShiftResource> Shifts, string Message)> ResolveFuzzyAsync(
        string searchString, int maxResults, (ShiftFilterType FilterType, bool IsSealedOrder) view,
        CancellationToken cancellationToken)
    {
        var catalogFilter = BuildFilter(string.Empty, FuzzyCatalogCap, view);
        var catalog = await _mediator.Send(new GetTruncatedListQuery(catalogFilter), cancellationToken);
        var catalogShifts = (catalog.Shifts ?? new List<ShiftResource>()).ToList();
        var truncationNote = catalog.MaxItems > catalogShifts.Count
            ? $" Note: only the first {catalogShifts.Count} of {catalog.MaxItems} shifts were considered."
            : string.Empty;

        var resolution = NameResolution.ResolveVariants<ShiftResource>(
            catalogShifts,
            s => [s.Name, s.Abbreviation],
            searchString,
            ShiftLabelWords);

        if (resolution.Match != null)
        {
            return ([resolution.Match],
                $"No direct match for '{searchString}'; closest shift is '{resolution.Match.Name}'. " +
                $"Tell the user you assumed this shift.{truncationNote}");
        }

        if (resolution.Suggestion != null)
        {
            return ([], NameResolutionMessages.SuggestedName(resolution.Suggestion.Name) + truncationNote);
        }

        if (resolution.Candidates.Count > 1)
        {
            var candidates = resolution.Candidates.Take(maxResults).ToList();
            return (candidates,
                $"'{searchString}' is ambiguous — closest shifts: " +
                string.Join(", ", candidates.Select(s => s.Name)) + ". " +
                $"Ask the user which one they mean — do not guess.{truncationNote}");
        }

        var sample = catalogShifts
            .Select(s => s.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Take(NotFoundNameSampleSize)
            .ToList();
        var available = sample.Count > 0
            ? " Available shifts include: " + string.Join(", ", sample) + ". " +
              "Offer the user only these real shift names — do not invent shifts."
            : " There are no shifts yet.";
        return ([], $"Found 0 shift(s) matching '{searchString}'.{available}{truncationNote}");
    }

    private static string? FormatClientName(ClientResource? client)
    {
        if (client == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(client.Company))
        {
            return client.Company;
        }

        var fullName = $"{client.FirstName} {client.Name}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? null : fullName;
    }
}
