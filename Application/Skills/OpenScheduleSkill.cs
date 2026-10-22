// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that navigates to the schedule (Dienstplan / shift planner) page, optionally
/// pre-filtered to a specific group, scoped to one person and opened on the period of a given
/// day. When the user just edited an employee that was assigned to a group, pass that group's
/// name so the schedule opens with the correct group filter. The person (clientId, or first and
/// last name) and the day (date) become the groupId / clientId / date query parameters the
/// schedule page reads. A person scope replaces the group filter, exactly like the page does.
/// A client hidden from the caller by group visibility is answered like an unknown client. The page narrows the
/// schedule to a person through the client's ID number, so a client without one opens the schedule unfiltered and
/// the answer says so.
/// </summary>
/// <param name="groupSearchRepository">Resolves group name to id via the same search index used elsewhere</param>
/// <param name="clientRepository">Loads the client behind a clientId or a resolved name</param>
/// <param name="searchRepository">Resolves a client by first and last name, scoped to the caller's visible groups</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see a client given by id</param>
/// <param name="companyClock">Company calendar day, the anchor for relative day words such as "today"</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("open_schedule")]
public class OpenScheduleSkill : BaseSkillImplementation
{
    private const string ScheduleRoute = "/workplace/schedule";
    private const string GroupIdQueryParam = "groupId";
    private const string ClientIdQueryParam = "clientId";
    private const string DateQueryParam = "date";
    private const int GroupSearchLimit = 10;

    private readonly IGroupSearchRepository _groupSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ICompanyClock _companyClock;

    public OpenScheduleSkill(
        IGroupSearchRepository groupSearchRepository,
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ICompanyClock companyClock)
    {
        _groupSearchRepository = groupSearchRepository;
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dateRaw = GetParameter<string>(parameters, "date");
        var today = string.IsNullOrWhiteSpace(dateRaw)
            ? default
            : await _companyClock.GetTodayAsync(cancellationToken);
        var (date, invalidDate) = SkillDateParser.ParseOptionalUtcDate(dateRaw, today, context.UserLanguage);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor("date", dateRaw!));
        }

        var dateText = date?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);

        var (client, clientError) = await ResolveClientAsync(parameters, cancellationToken);
        if (clientError != null)
        {
            return SkillResult.Error(clientError);
        }

        if (client != null)
        {
            return client.IdNumber > 0
                ? OpenForClient(client, dateText)
                : OpenWithoutPersonFilter(client, dateText);
        }

        var groupName = GetParameter<string>(parameters, "groupName");
        if (string.IsNullOrWhiteSpace(groupName))
        {
            return OpenWithoutGroup(dateText, "Open schedule");
        }

        var result = await _groupSearchRepository.SearchAsync(
            searchTerm: groupName,
            limit: GroupSearchLimit,
            cancellationToken: cancellationToken);

        if (result.TotalCount == 0)
        {
            return OpenWithoutGroup(
                dateText, $"No group found matching '{groupName}'. Opening schedule without group filter.");
        }

        if (result.TotalCount > 1)
        {
            var matches = result.Items
                .Select(g => new { g.Id, g.Name })
                .ToList();

            return new SkillResult
            {
                Success = true,
                Data = new { EntityType = "group", Matches = matches, Count = matches.Count },
                Message = $"Multiple groups match '{groupName}'. Please specify which one to open the schedule for.",
                Type = SkillResultType.Data
            };
        }

        var group = result.Items.First();
        var route = BuildRoute((GroupIdQueryParam, group.Id.ToString()), (DateQueryParam, dateText));

        return SkillResult.Navigation(
            new { Route = route, GroupId = group.Id, GroupName = group.Name, Date = dateText },
            $"Open schedule for group '{group.Name}'" + DayNote(dateText));
    }

    private async Task<(Client? Client, string? Error)> ResolveClientAsync(
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var clientIdRaw = GetParameter<string>(parameters, "clientId");
        if (!string.IsNullOrWhiteSpace(clientIdRaw))
        {
            if (!Guid.TryParse(clientIdRaw, out var clientId))
            {
                return (null, $"Invalid client ID format: {clientIdRaw}");
            }

            var byId = await ClientResolver.LoadVisibleByIdAsync(
                _clientRepository, _clientVisibilityGuard, clientId, cancellationToken);
            return byId == null ? (null, $"Client with ID {clientId} not found.") : (byId, null);
        }

        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return (null, null);
        }

        var idNumber = GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName);
        return await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, idNumber, cancellationToken);
    }

    private static SkillResult OpenForClient(Client client, string? dateText)
    {
        var route = BuildRoute((ClientIdQueryParam, client.Id.ToString()), (DateQueryParam, dateText));
        var name = $"{client.FirstName} {client.Name}".Trim();

        return SkillResult.Navigation(
            new { Route = route, ClientId = client.Id, ClientName = name, Date = dateText },
            $"Open schedule for '{name}'" + DayNote(dateText));
    }

    private static SkillResult OpenWithoutPersonFilter(Client client, string? dateText)
    {
        var route = BuildRoute((DateQueryParam, dateText));
        var name = $"{client.FirstName} {client.Name}".Trim();

        return SkillResult.Navigation(
            new { Route = route, ClientId = client.Id, ClientName = name, Date = dateText, PersonFilterApplied = false },
            $"'{name}' has no ID number, so the schedule cannot be narrowed to this person. " +
            "Opening the schedule without a person filter" + DayNote(dateText));
    }

    private static SkillResult OpenWithoutGroup(string? dateText, string message)
    {
        var route = BuildRoute((DateQueryParam, dateText));

        return SkillResult.Navigation(
            new { Route = route, GroupId = (Guid?)null, GroupName = (string?)null, Date = dateText },
            message + DayNote(dateText));
    }

    private static string BuildRoute(params (string Name, string? Value)[] queryParts)
    {
        var parts = queryParts
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => $"{p.Name}={Uri.EscapeDataString(p.Value!)}")
            .ToList();

        return parts.Count == 0 ? ScheduleRoute : $"{ScheduleRoute}?{string.Join('&', parts)}";
    }

    private static string DayNote(string? dateText) =>
        dateText == null ? string.Empty : $", on the period containing {dateText}";
}
