// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises on what hangs on the exit of one employee at a date: whether recording it would be refused (no, several or a
/// not yet started active membership), what stays booked after the exit date in the main plan (work, absences, on-call
/// duty, entries on approved or closed days), contracts that do not end on the exit date and the groups the employee
/// belongs to. The membership rule is the one of the recording skill (MembershipExitRules); the employee is resolved
/// like a write would, so the advice is about the person the recording would hit. Read-only advice computed in C#, no
/// pay or hours figures; recording the exit is a separate step the system confirms once (the AdvisesFor target is
/// Sensitive). Bookings are looked for up to EmployeeExitLimits.HorizonDays after the exit date.
/// </summary>
/// <param name="exitDate">Required. The last day of employment to evaluate (YYYY-MM-DD).</param>
/// <param name="clientId">Optional. UUID of the employee; takes precedence over the names.</param>
/// <param name="firstName">Optional. First name of the employee when clientId is unknown.</param>
/// <param name="lastName">Optional. Last name of the employee when clientId is unknown.</param>
/// <param name="idNumber">Optional. Client number to tell apart employees with the same name.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Clients;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Clients;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateEmployeeExitSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_employee_exit";

    private const string ExitDateParameter = "exitDate";
    private const string ClientIdParameter = "clientId";
    private const string FirstNameParameter = "firstName";
    private const string LastNameParameter = "lastName";

    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IClientContractReadRepository _contractReadRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly ICompanyClock _companyClock;

    public EvaluateEmployeeExitSkill(
        IClientSearchRepository clientSearchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IMembershipRepository membershipRepository,
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IAbsenceRepository absenceRepository,
        IClientContractReadRepository contractReadRepository,
        IGroupItemRepository groupItemRepository,
        ICompanyClock companyClock)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _membershipRepository = membershipRepository;
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _absenceRepository = absenceRepository;
        _contractReadRepository = contractReadRepository;
        _groupItemRepository = groupItemRepository;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var exitRaw = GetParameter<string>(parameters, ExitDateParameter);
        if (!SkillCalendarStringParser.TryParseDateOnly(exitRaw, context.UserLanguage, out var exitDate))
        {
            return SkillResult.Error($"Invalid or missing {ExitDateParameter} '{exitRaw}'. Expected the last day of employment, for example 2026-03-31.");
        }

        var clientIdRaw = GetParameter<string>(parameters, ClientIdParameter);
        var firstName = GetParameter<string>(parameters, FirstNameParameter);
        var lastName = GetParameter<string>(parameters, LastNameParameter);
        var (client, error) = await ClientResolver.ResolveByIdOrNameAsync(
            _clientSearchRepository, _clientRepository, _clientVisibilityGuard, clientIdRaw, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken);
        if (client == null || !await _clientVisibilityGuard.IsVisibleAsync(client.Id, cancellationToken))
        {
            return SkillResult.Error(error ?? ClientResolver.NotFoundMessage(firstName, lastName));
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var horizonEnd = exitDate.AddDays(EmployeeExitLimits.HorizonDays);
        var activeMemberships = MembershipExitRules.ActiveMemberships(
            await _membershipRepository.ListByClientAsync(client.Id, cancellationToken), client.Id, today);

        var facts = new EmployeeExitFacts(
            $"{client.FirstName} {client.Name}".Trim(),
            exitDate,
            horizonEnd,
            BuildMembershipFacts(activeMemberships),
            await ReadPlanFactsAsync(client.Id, exitDate, horizonEnd, DateOnly.FromDateTime(today), cancellationToken),
            await ReadContractFactsAsync(client.Id, exitDate, cancellationToken),
            (await _groupItemRepository.GetVisibilityMembershipAsync(client.Id, cancellationToken))?.ActiveGroupIds.Count ?? 0);

        var evaluation = EmployeeExitAdvisor.Evaluate(client.Id, facts);

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }

    private static EmployeeExitMembershipFacts BuildMembershipFacts(IReadOnlyList<Membership> activeMemberships)
    {
        if (activeMemberships.Count == 1)
        {
            var lone = activeMemberships[0];
            return new EmployeeExitMembershipFacts(
                1,
                DateOnly.FromDateTime(lone.ValidFrom),
                lone.ValidUntil.HasValue ? DateOnly.FromDateTime(lone.ValidUntil.Value) : null,
                string.Empty);
        }

        return new EmployeeExitMembershipFacts(
            activeMemberships.Count,
            null,
            null,
            activeMemberships.Count > 1 ? MembershipExitRules.DescribeOptions(activeMemberships) : string.Empty);
    }

    private async Task<EmployeeExitPlanFacts> ReadPlanFactsAsync(
        Guid clientId, DateOnly exitDate, DateOnly horizonEnd, DateOnly today, CancellationToken cancellationToken)
    {
        var firstDay = exitDate.AddDays(1);
        var works = MainPlanWorks.ParentsOnly(await _workRepository.GetByClientAndDateRangeAsync(
            clientId,
            firstDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            horizonEnd.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            cancellationToken));
        var breaks = (await _breakRepository.GetByClientAndDateRangeAsync(clientId, firstDay, horizonEnd, cancellationToken))
            .Where(entry => entry.AnalyseToken == null && entry.ParentWorkId == null)
            .ToList();
        var onCallAbsenceIds = await _absenceRepository.GetOnCallAbsenceIdsAsync(cancellationToken);
        var onCall = breaks.Where(entry => onCallAbsenceIds.Contains(entry.AbsenceId)).ToList();
        var absences = breaks.Except(onCall).ToList();
        var lockLevels = works.Select(work => work.LockLevel).Concat(breaks.Select(entry => entry.LockLevel)).ToList();

        return new EmployeeExitPlanFacts(
            works.Count,
            works.Count(work => work.CurrentDate < today),
            works.Count > 0 ? works.Min(work => work.CurrentDate) : null,
            works.Count > 0 ? works.Max(work => work.CurrentDate) : null,
            absences.Select(entry => entry.CurrentDate).Distinct().Count(),
            onCall.Select(entry => entry.CurrentDate).Distinct().Count(),
            lockLevels.Count(level => level == WorkLockLevel.Approved),
            lockLevels.Count(level => level == WorkLockLevel.Closed));
    }

    private async Task<EmployeeExitContractFacts> ReadContractFactsAsync(
        Guid clientId, DateOnly exitDate, CancellationToken cancellationToken)
    {
        var contracts = (await _contractReadRepository.GetContractsForClientAsync(clientId, cancellationToken))
            .Where(contract => contract.IsActive && !contract.IsDeleted)
            .ToList();
        var covering = contracts.Where(contract => ValidOn(contract, exitDate)).ToList();
        var endedBefore = contracts
            .Where(contract => contract.UntilDate.HasValue && contract.UntilDate.Value < exitDate)
            .Select(contract => contract.UntilDate)
            .ToList();

        return new EmployeeExitContractFacts(
            covering.Count(contract => !contract.UntilDate.HasValue || contract.UntilDate.Value > exitDate),
            covering.Count > 0,
            covering.Count == 0 && endedBefore.Count > 0 ? endedBefore.Max() : null);
    }

    private static bool ValidOn(ClientContract contract, DateOnly day) =>
        contract.FromDate <= day && (!contract.UntilDate.HasValue || contract.UntilDate.Value >= day);
}
