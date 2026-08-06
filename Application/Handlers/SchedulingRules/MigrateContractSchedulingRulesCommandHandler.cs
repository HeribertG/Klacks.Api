// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the admin's migration decisions from the industry migration list. Each contract is moved
/// individually and only when the target actually differs, and every moved contract raises a
/// ContractChangedEvent so the surcharge recalculation reacts exactly as it does for a normal
/// contract edit. Unknown contract ids are skipped rather than failing the batch, so one stale row
/// in the list cannot block the remaining migrations.
/// </summary>
/// <param name="request">The contract-to-rule assignments the admin confirmed</param>

using Klacks.Api.Application.Commands.SchedulingRules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SchedulingRules;

public class MigrateContractSchedulingRulesCommandHandler
    : BaseHandler, IRequestHandler<MigrateContractSchedulingRulesCommand, int>
{
    private readonly IContractRepository _contractRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public MigrateContractSchedulingRulesCommandHandler(
        IContractRepository contractRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<MigrateContractSchedulingRulesCommandHandler> logger)
        : base(logger)
    {
        _contractRepository = contractRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<int> Handle(MigrateContractSchedulingRulesCommand request, CancellationToken cancellationToken)
    {
        var migrated = new List<(Guid ContractId, DateOnly RecalculateFrom)>();

        var result = await ExecuteAsync(async () =>
        {
            foreach (var assignment in request.Assignments)
            {
                var contract = await _contractRepository.Get(assignment.ContractId);
                if (contract == null)
                {
                    _logger.LogWarning(
                        "Skipping migration of contract {ContractId}: it no longer exists.",
                        assignment.ContractId);
                    continue;
                }

                if (contract.SchedulingRuleId == assignment.SchedulingRuleId)
                {
                    continue;
                }

                contract.SchedulingRuleId = assignment.SchedulingRuleId;
                migrated.Add((contract.Id, DateOnly.FromDateTime(contract.ValidFrom)));
            }

            if (migrated.Count > 0)
            {
                await _unitOfWork.CompleteAsync();
            }

            return migrated.Count;
        },
        "migrating contract scheduling rules",
        new { AssignmentCount = request.Assignments.Count });

        foreach (var (contractId, recalculateFrom) in migrated)
        {
            await DispatchContractChangedAsync(contractId, recalculateFrom);
        }

        return result;
    }

    private async Task DispatchContractChangedAsync(Guid contractId, DateOnly recalculateFrom)
    {
        try
        {
            await _eventDispatcher.DispatchAsync(
                new ContractChangedEvent(contractId, null, recalculateFrom, null),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed for contract {ContractId}; the migration is persisted and remains unaffected.",
                nameof(ContractChangedEvent),
                contractId);
        }
    }
}
