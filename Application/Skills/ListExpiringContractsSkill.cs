// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only expiry sweep: lists client contract assignments and (optionally) client qualifications
/// that expire within the next N days, relative to the company's current date. Only Employee and
/// ExternEmp clients are considered — Customer rows do not hold employment contracts or work
/// qualifications in this sense. Contract assignments already superseded by a newer one
/// (IsActive false, set by assign_contract_to_client / assign_contract_by_name) are excluded, since
/// they are historical rows, not a contract about to lapse. Results are combined and sorted by
/// expiry date ascending.
/// </summary>
/// <param name="withinDays">Optional. Horizon in days from today; defaults to 90.</param>
/// <param name="includeQualifications">Optional. Whether to include qualification expiries; defaults to true.</param>
/// <param name="limit">Optional. Maximum number of combined results to return; defaults to 50.</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_expiring_contracts")]
public class ListExpiringContractsSkill : BaseSkillImplementation
{
    private const int DefaultWithinDays = 90;
    private const int DefaultLimit = 50;
    private const string ContractKind = "Contract";
    private const string QualificationKind = "Qualification";
    private const string UnknownContractName = "Unknown contract";
    private const string UnknownQualificationName = "Unknown qualification";

    private readonly IClientContractReadRepository _clientContractReadRepository;
    private readonly IContractRepository _contractRepository;
    private readonly IClientQualificationRepository _clientQualificationRepository;
    private readonly IQualificationRepository _qualificationRepository;
    private readonly IClientRepository _clientRepository;
    private readonly ICompanyClock _companyClock;

    public ListExpiringContractsSkill(
        IClientContractReadRepository clientContractReadRepository,
        IContractRepository contractRepository,
        IClientQualificationRepository clientQualificationRepository,
        IQualificationRepository qualificationRepository,
        IClientRepository clientRepository,
        ICompanyClock companyClock)
    {
        _clientContractReadRepository = clientContractReadRepository;
        _contractRepository = contractRepository;
        _clientQualificationRepository = clientQualificationRepository;
        _qualificationRepository = qualificationRepository;
        _clientRepository = clientRepository;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var withinDays = GetParameter<int?>(parameters, "withinDays") ?? DefaultWithinDays;
        if (withinDays <= 0)
        {
            return SkillResult.Error("withinDays must be a positive number of days.");
        }

        var includeQualifications = GetParameter<bool?>(parameters, "includeQualifications") ?? true;

        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit <= 0)
        {
            return SkillResult.Error("limit must be a positive number.");
        }

        var todayUtc = await _companyClock.GetTodayAsync(cancellationToken);
        var today = DateOnly.FromDateTime(todayUtc);
        var horizon = today.AddDays(withinDays);

        var contractItems = await BuildExpiringContractItemsAsync(today, horizon, cancellationToken);

        var qualificationItems = includeQualifications
            ? await BuildExpiringQualificationItemsAsync(today, horizon, cancellationToken)
            : new List<ExpiringItem>();

        var combined = contractItems
            .Concat(qualificationItems)
            .OrderBy(i => i.ValidUntil)
            .ThenBy(i => i.ClientName, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToList();

        var result = new ListExpiringContractsResult(
            withinDays,
            includeQualifications,
            contractItems.Count,
            qualificationItems.Count,
            combined);

        if (contractItems.Count == 0 && qualificationItems.Count == 0)
        {
            var nothingMessage = includeQualifications
                ? $"No contracts or qualifications expire within the next {withinDays} days."
                : $"No contracts expire within the next {withinDays} days.";
            return SkillResult.SuccessResult(result, nothingMessage);
        }

        var summary = includeQualifications
            ? $"{contractItems.Count} contract(s) and {qualificationItems.Count} qualification(s) expire within {withinDays} days, earliest first."
            : $"{contractItems.Count} contract(s) expire within {withinDays} days, earliest first.";

        return SkillResult.SuccessResult(
            result,
            $"{summary} Relay each client's name, the expiring item and its expiry date to the user.");
    }

    private async Task<List<ExpiringItem>> BuildExpiringContractItemsAsync(
        DateOnly today, DateOnly horizon, CancellationToken cancellationToken)
    {
        var expiringContracts = await _clientContractReadRepository.GetExpiringBetweenAsync(today, horizon, cancellationToken);
        if (expiringContracts.Count == 0)
        {
            return new List<ExpiringItem>();
        }

        var contractNames = (await _contractRepository.List())
            .ToDictionary(c => c.Id, c => c.Name);

        return expiringContracts
            .Where(c => c.IsActive && c.UntilDate.HasValue && c.Client != null && IsRelevantEntityType(c.Client.Type))
            .Select(c => new ExpiringItem(
                c.ClientId,
                FormatClientName(c.Client!),
                ContractKind,
                contractNames.TryGetValue(c.ContractId, out var contractName) ? contractName : UnknownContractName,
                c.UntilDate!.Value,
                c.UntilDate!.Value.DayNumber - today.DayNumber))
            .ToList();
    }

    private async Task<List<ExpiringItem>> BuildExpiringQualificationItemsAsync(
        DateOnly today, DateOnly horizon, CancellationToken cancellationToken)
    {
        var allQualificationLinks = await _clientQualificationRepository.List();
        var expiringLinks = allQualificationLinks
            .Where(q => q.ValidUntil.HasValue && q.ValidUntil.Value >= today && q.ValidUntil.Value <= horizon)
            .ToList();

        if (expiringLinks.Count == 0)
        {
            return new List<ExpiringItem>();
        }

        var clientIds = expiringLinks.Select(q => q.ClientId).Distinct().ToList();
        var clientsById = (await _clientRepository.GetByIdsAsync(clientIds, cancellationToken))
            .ToDictionary(c => c.Id);

        var qualificationsById = (await _qualificationRepository.GetAllAsync(cancellationToken))
            .ToDictionary(q => q.Id);

        return expiringLinks
            .Where(q => clientsById.ContainsKey(q.ClientId) && IsRelevantEntityType(clientsById[q.ClientId].Type))
            .Select(q =>
            {
                var client = clientsById[q.ClientId];
                var qualificationName = qualificationsById.TryGetValue(q.QualificationId, out var qualification)
                    ? QualificationResolver.DisplayName(qualification)
                    : UnknownQualificationName;

                return new ExpiringItem(
                    q.ClientId,
                    FormatClientName(client),
                    QualificationKind,
                    qualificationName,
                    q.ValidUntil!.Value,
                    q.ValidUntil!.Value.DayNumber - today.DayNumber);
            })
            .ToList();
    }

    private static bool IsRelevantEntityType(EntityTypeEnum type)
        => type == EntityTypeEnum.Employee || type == EntityTypeEnum.ExternEmp;

    private static string FormatClientName(Client client)
        => $"{client.FirstName} {client.Name}".Trim();
}
