// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records an employee exit by setting validUntil on one membership of a client without requiring
/// the caller to know a membership UUID: resolves the client's single active membership (validUntil
/// null or not in the past) and sets its validUntil to the exit date inside a transaction with
/// database verification. With zero active memberships it fails; with more than one it fails and
/// lists the real options instead of guessing (lone-match rule). An explicit membershipId skips the
/// resolution and ends exactly that membership of the client.
/// </summary>
/// <param name="clientId">Required. UUID of the client whose membership is ended.</param>
/// <param name="exitDate">Required. Last day of employment (YYYY-MM-DD); becomes the membership's validUntil.</param>
/// <param name="membershipId">Optional. UUID of the membership to end when the client has more than one active membership.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

using Klacks.Api.Application.DTOs.Associations;

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EndClientMembershipSkill : BaseSkillImplementation
{
    private const string SkillName = "end_client_membership";
    private const string OpenEndedLabel = "open-ended";
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IMembershipRepository _membershipRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IKlacksSelfApiClient _selfApi;
    private readonly ISelfApiRouteResolver _routes;
    private readonly ICompanyClock _companyClock;

    public EndClientMembershipSkill(
        IMembershipRepository membershipRepository,
        IClientRepository clientRepository,
        IKlacksSelfApiClient selfApi,
        ISelfApiRouteResolver routes,
        ICompanyClock companyClock)
    {
        _membershipRepository = membershipRepository;
        _clientRepository = clientRepository;
        _selfApi = selfApi;
        _routes = routes;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var exitDate = GetParameter<DateOnly?>(parameters, "exitDate")
            ?? throw new ArgumentException("Required parameter 'exitDate' is missing");

        var membershipIdRaw = GetParameter<string>(parameters, "membershipId");
        Guid? membershipIdOverride = null;
        if (!string.IsNullOrWhiteSpace(membershipIdRaw))
        {
            if (!Guid.TryParse(membershipIdRaw, out var parsedMembershipId))
            {
                return SkillResult.Error($"Invalid membershipId format: {membershipIdRaw}");
            }

            membershipIdOverride = parsedMembershipId;
        }

        if (!await _clientRepository.Exists(clientId))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var memberships = await _membershipRepository.List();

        Membership membership;
        if (membershipIdOverride.HasValue)
        {
            var requested = memberships.FirstOrDefault(m => m.Id == membershipIdOverride.Value);
            if (requested == null)
            {
                return SkillResult.Error($"Membership '{membershipIdOverride.Value}' not found.");
            }

            if (requested.ClientId != clientId)
            {
                return SkillResult.Error(
                    $"Membership '{membershipIdOverride.Value}' does not belong to client {clientId}. " +
                    "Use list_client_memberships to find the client's memberships.");
            }

            membership = requested;
        }
        else
        {
            var today = await _companyClock.GetTodayAsync(cancellationToken);
            var activeMemberships = memberships
                .Where(m => m.ClientId == clientId)
                .Where(m => m.ValidUntil == null || m.ValidUntil.Value.Date >= today.Date)
                .OrderBy(m => m.ValidFrom)
                .ToList();

            if (activeMemberships.Count == 0)
            {
                return SkillResult.Error(
                    $"Client {clientId} has no active membership — nothing to end. " +
                    "Use list_client_memberships to inspect the membership history.");
            }

            if (activeMemberships.Count > 1)
            {
                var options = string.Join("; ", activeMemberships.Select(m =>
                    $"id={m.Id}, validFrom={m.ValidFrom.ToString(DateFormat)}, " +
                    $"validUntil={FormatValidUntil(m.ValidUntil)}, type={m.Type}"));
                return SkillResult.Error(
                    $"Client {clientId} has {activeMemberships.Count} active memberships — cannot decide which one to end. " +
                    $"Available memberships: {options}. Ask the user which membership applies, then call " +
                    "end_client_membership again with the membershipId parameter set to the chosen membership.");
            }

            membership = activeMemberships[0];
        }

        var exitDateTime = exitDate.ToDateTime(TimeOnly.MinValue);
        if (exitDateTime < membership.ValidFrom.Date)
        {
            return SkillResult.Error(
                $"exitDate {exitDate.ToString(DateFormat)} must not be before the membership's " +
                $"validFrom {membership.ValidFrom.ToString(DateFormat)}.");
        }

        var resource = new MembershipResource
        {
            Id = membership.Id,
            ClientId = membership.ClientId,
            Type = membership.Type,
            ValidFrom = membership.ValidFrom,
            ValidUntil = exitDateTime
        };

        var result = await _selfApi.PutAsync<MembershipResource>(
            _routes.Resolve(typeof(MembershipResource)), resource, context, SkillName, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.ErrorMessage!);
        }

        membership.ValidUntil = exitDateTime;

        var firstUnplannableDay = exitDate.AddDays(1);

        return SkillResult.SuccessResult(
            new
            {
                ClientId = clientId,
                MembershipId = membership.Id,
                membership.ValidFrom,
                ValidUntil = exitDateTime,
                ExitDate = exitDate,
                FirstUnplannableDay = firstUnplannableDay
            },
            $"Recorded the exit for client {clientId}: validUntil of membership '{membership.Id}' was set to " +
            $"{exitDate.ToString(DateFormat)} and confirmed in the database (verified). The exit date is the last " +
            $"day of employment, so the client can no longer be planned in the schedule from " +
            $"{firstUnplannableDay.ToString(DateFormat)} on (membership boundary).");
    }

    private static string FormatValidUntil(DateTime? validUntil)
        => validUntil.HasValue ? validUntil.Value.ToString(DateFormat) : OpenEndedLabel;
}
