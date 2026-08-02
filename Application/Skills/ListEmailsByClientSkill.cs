// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the received emails on file for one client, resolved by name via
/// GetEmailsByClientQuery, as compact sender/subject/date projections.
/// </summary>
/// <param name="firstName">Optional. First name of the client.</param>
/// <param name="lastName">Last name of the client (required).</param>
/// <param name="idNumber">Optional visible client number to disambiguate duplicate names.</param>
/// <param name="maxResults">Optional. Maximum number of emails to return (1-50); defaults to 20.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_emails_by_client")]
public class ListEmailsByClientSkill : BaseSkillImplementation
{
    private const string FirstNameParameter = "firstName";
    private const string LastNameParameter = "lastName";
    private const string MaxResultsParameter = "maxResults";
    private const int DefaultMaxResults = 20;
    private const int MinResults = 1;
    private const int MaxResults = 50;
    private const int SkipNone = 0;

    private readonly IClientSearchRepository _searchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IMediator _mediator;

    public ListEmailsByClientSkill(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        IMediator mediator)
    {
        _searchRepository = searchRepository;
        _clientRepository = clientRepository;
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var firstName = GetParameter<string>(parameters, FirstNameParameter);
        var lastName = GetRequiredString(parameters, LastNameParameter);
        var idNumber = GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName);

        var (client, error) = await ClientResolver.ResolveByNameAsync(
            _searchRepository, _clientRepository, firstName, lastName, idNumber, cancellationToken);
        if (error != null)
        {
            return SkillResult.Error(error);
        }

        var take = Math.Clamp(
            GetParameter<int?>(parameters, MaxResultsParameter) ?? DefaultMaxResults, MinResults, MaxResults);

        var response = await _mediator.Send(
            new GetEmailsByClientQuery(client!.Id, SkipNone, take), cancellationToken);

        var projected = response.Items
            .Select(e => new
            {
                e.Id,
                From = FormatSender(e.FromName, e.FromAddress),
                e.Subject,
                Received = e.ReceivedDate,
                e.IsRead,
                e.HasAttachments
            })
            .ToList();

        return SkillResult.SuccessResult(
            new
            {
                ClientId = client.Id,
                ClientName = $"{client.FirstName} {client.Name}",
                Count = projected.Count,
                response.TotalCount,
                response.UnreadCount,
                Emails = projected
            },
            $"{client.FirstName} {client.Name} has {response.TotalCount} email(s) on file " +
            $"({response.UnreadCount} unread). Showing {projected.Count}.");
    }

    private static string FormatSender(string? fromName, string fromAddress)
    {
        return string.IsNullOrWhiteSpace(fromName) ? fromAddress : $"{fromName} <{fromAddress}>";
    }
}
