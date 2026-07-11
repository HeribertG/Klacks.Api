// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Searches shift definitions by an optional search string and returns a compact list
/// (id, name, abbreviation, validity dates, start/end times, client name). Thin wrapper
/// around <see cref="Klacks.Api.Application.Queries.Shifts.GetTruncatedListQuery"/> that
/// covers active, former and future shifts. When the database text filter finds nothing,
/// a fuzzy fallback ranks the spoken or decorated query ("Dienst All-Shift") against the
/// real shift names and abbreviations in memory and suggests the closest shifts. Use this
/// to find a shiftId for get_shift_details / update_shift / delete_shift.
/// </summary>
/// <param name="searchString">Optional. Text filter on shift name, abbreviation or client name;
/// falls back to fuzzy name resolution when the filter finds nothing.</param>
/// <param name="maxResults">Optional. Maximum number of shifts to return (default 20, capped at 100).</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
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

    private static readonly string[] ShiftLabelWords = ["dienst", "schicht", "shift", "service", "turno"];

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

        var filter = new ShiftFilter
        {
            SearchString = searchString,
            NumberOfItemsPerPage = maxResults,
            ActiveDateRange = true,
            FormerDateRange = true,
            FutureDateRange = true,
            IncludeClientName = true
        };

        var result = await _mediator.Send(new GetTruncatedListQuery(filter), cancellationToken);

        var totalCount = result.MaxItems;
        var matched = result.Shifts ?? new List<ShiftResource>();
        var message = $"Found {totalCount} shift(s)" +
                      (!string.IsNullOrWhiteSpace(searchString) ? $" matching '{searchString}'" : string.Empty) +
                      $"; returning {matched.Count}.";

        // A spoken or decorated name ("Dienst All-Shift") rarely survives the database text
        // filter — rank it against the real shift names instead of reporting an empty list.
        if (totalCount == 0 && !string.IsNullOrWhiteSpace(searchString))
        {
            (matched, message) = await ResolveFuzzyAsync(searchString, maxResults, cancellationToken);
            totalCount = matched.Count;
        }

        var shifts = matched
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Abbreviation,
                s.FromDate,
                s.UntilDate,
                s.StartShift,
                s.EndShift,
                ClientName = FormatClientName(s.Client)
            })
            .ToList();

        return SkillResult.SuccessResult(new { TotalCount = totalCount, Shifts = shifts }, message);
    }

    private async Task<(List<ShiftResource> Shifts, string Message)> ResolveFuzzyAsync(
        string searchString, int maxResults, CancellationToken cancellationToken)
    {
        var catalogFilter = new ShiftFilter
        {
            SearchString = string.Empty,
            NumberOfItemsPerPage = FuzzyCatalogCap,
            ActiveDateRange = true,
            FormerDateRange = true,
            FutureDateRange = true,
            IncludeClientName = true
        };
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
