// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that searches for client/employee absences within a time period.
/// Supports filtering by client name, absence type, year, date range and group.
/// Returns clients with their absence periods (break placeholders and schedule breaks merged).
/// </summary>
/// <param name="searchTerm">Optional client name or company to filter by</param>
/// <param name="year">Year to search in (default: current year)</param>
/// <param name="startDate">Optional start date for date range filter (overrides year)</param>
/// <param name="endDate">Optional end date for date range filter (overrides year)</param>
/// <param name="absenceType">Optional absence type name to filter by</param>
/// <param name="limit">Maximum number of clients to return (default: 10)</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("search_client_absences")]
public class SearchClientAbsencesSkill : BaseSkillImplementation
{
    private readonly IClientBreakPlaceholderRepository _breakRepository;
    private readonly IAbsenceRepository _absenceRepository;

    private const int MaxLimit = 50;
    private const int DefaultLimit = 10;

    public SearchClientAbsencesSkill(
        IClientBreakPlaceholderRepository breakRepository,
        IAbsenceRepository absenceRepository)
    {
        _breakRepository = breakRepository;
        _absenceRepository = absenceRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var year = GetParameter<int?>(parameters, "year") ?? DateTime.UtcNow.Year;
        var startDateParam = GetParameter<string>(parameters, "startDate");
        var endDateParam = GetParameter<string>(parameters, "endDate");
        var absenceTypeName = GetParameter<string>(parameters, "absenceType");
        var limit = Math.Min(GetParameter<int?>(parameters, "limit") ?? DefaultLimit, MaxLimit);

        var allAbsences = await _absenceRepository.List();
        var visibleAbsences = allAbsences
            .Where(a => !a.IsDeleted && !a.HideInGantt)
            .ToList();

        var absenceIds = visibleAbsences.Select(a => a.Id).ToList();

        if (!string.IsNullOrWhiteSpace(absenceTypeName))
        {
            var matchingAbsences = visibleAbsences
                .Where(a => MatchesAbsenceType(a, absenceTypeName))
                .ToList();

            if (matchingAbsences.Count == 0)
            {
                return SkillResult.Error($"No absence type found matching '{absenceTypeName}'.");
            }

            absenceIds = matchingAbsences.Select(a => a.Id).ToList();
        }

        var filter = new BreakFilter
        {
            SearchString = searchTerm ?? string.Empty,
            CurrentYear = year,
            AbsenceIds = absenceIds,
            ShowEmployees = true,
            ShowExtern = true,
            OrderBy = "name",
            SortOrder = "asc",
            StartRow = 0,
            RowCount = limit
        };

        if (TryParseDateOnly(startDateParam, out var startDate))
        {
            filter.StartDate = startDate;
        }

        if (TryParseDateOnly(endDateParam, out var endDate))
        {
            filter.EndDate = endDate;
        }

        var (clients, totalCount) = await _breakRepository.BreakList(filter);

        var absenceLookup = visibleAbsences.ToDictionary(a => a.Id);

        var results = clients.Select(c => new
        {
            ClientId = c.Id,
            c.IdNumber,
            c.FirstName,
            LastName = c.Name,
            c.Company,
            Absences = c.BreakPlaceholders
                .Where(bp => !bp.IsDeleted)
                .OrderBy(bp => bp.From)
                .Select(bp => new
                {
                    AbsenceType = absenceLookup.TryGetValue(bp.AbsenceId, out var absence)
                        ? (absence.Name.De ?? absence.Name.En ?? "Unknown")
                        : "Unknown",
                    From = bp.From.ToString("yyyy-MM-dd"),
                    Until = bp.Until.ToString("yyyy-MM-dd"),
                    bp.Information
                })
                .ToList()
        })
        .Where(c => c.Absences.Count > 0)
        .ToList();

        var resultData = new
        {
            Results = results,
            Count = results.Count,
            TotalCount = totalCount,
            HasMore = totalCount > limit,
            Year = year,
            DateRange = startDate.HasValue && endDate.HasValue
                ? $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}"
                : (object?)null
        };

        var message = results.Count == 0
            ? "No absences found matching the criteria."
            : $"Found {results.Count} client(s) with absences" +
              (!string.IsNullOrWhiteSpace(searchTerm) ? $" matching '{searchTerm}'" : "") +
              (!string.IsNullOrWhiteSpace(absenceTypeName) ? $" of type '{absenceTypeName}'" : "") +
              (startDate.HasValue ? $" from {startDate:yyyy-MM-dd}" : $" in {year}") +
              (endDate.HasValue ? $" to {endDate:yyyy-MM-dd}" : "") +
              (totalCount > limit ? $". Showing first {limit} of {totalCount}." : ".");

        return SkillResult.SuccessResult(resultData, message);
    }

    private static bool MatchesAbsenceType(Domain.Models.Schedules.Absence absence, string searchName)
    {
        var search = searchName.Trim();
        return ContainsIgnoreCase(absence.Name.De, search) ||
               ContainsIgnoreCase(absence.Name.En, search) ||
               ContainsIgnoreCase(absence.Name.Fr, search) ||
               ContainsIgnoreCase(absence.Name.It, search) ||
               ContainsIgnoreCase(absence.Abbreviation.De, search) ||
               ContainsIgnoreCase(absence.Abbreviation.En, search);
    }

    private static bool ContainsIgnoreCase(string? source, string search)
    {
        return !string.IsNullOrEmpty(source) &&
               source.Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryParseDateOnly(string? value, out DateOnly? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (DateOnly.TryParse(value, out var parsed))
        {
            result = parsed;
            return true;
        }

        return false;
    }
}
