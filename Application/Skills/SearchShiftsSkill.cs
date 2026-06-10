// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Searches shift definitions by an optional search string and returns a compact list
/// (id, name, abbreviation, validity dates, start/end times, client name). Thin wrapper
/// around <see cref="Klacks.Api.Application.Queries.Shifts.GetTruncatedListQuery"/> that
/// covers active, former and future shifts. Use this to find a shiftId for
/// get_shift_details / update_shift / delete_shift.
/// </summary>
/// <param name="searchString">Optional. Text filter on shift name, abbreviation or client name.</param>
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

        var shifts = (result.Shifts ?? new List<ShiftResource>())
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

        var message = $"Found {result.MaxItems} shift(s)" +
                      (!string.IsNullOrWhiteSpace(searchString) ? $" matching '{searchString}'" : string.Empty) +
                      $"; returning {shifts.Count}.";

        return SkillResult.SuccessResult(new { TotalCount = result.MaxItems, Shifts = shifts }, message);
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
