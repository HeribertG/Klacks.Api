// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Orders;

/// <summary>
/// Handler for <see cref="SealOpenOrdersCommand"/>. Sealing runs through <see cref="IOrderSealingService"/>,
/// the same path seal_shift uses, once per order — that service opens the transaction, so the loop must not
/// wrap one around it and every order commits or rolls back on its own. A refusal or exception is recorded
/// against that order and the batch continues. The optional group assignment is sent as its own command
/// before the loop, never inside it, because it opens a transaction of its own as well. Every order outside
/// request.Scope is dropped right after loading, before anything is classified, counted or named, so a preview and
/// an apply - inline or as the deferred background job - see exactly the same scoped set, re-evaluated against the
/// orders as they are when the command runs. An apply that carries the confirmed preview (ExpectedSelection) first
/// classifies the matched orders again, before the group assignment or any seal writes, and throws
/// InvalidRequestException with the binding mismatch text when they no longer classify exactly as shown; afterwards it
/// only ever looks at the confirmed orders, so an order that appeared in the meantime is never sealed. The group assignment works on orders without any group, which only full
/// access may touch, so it is refused for every other scope.
/// </summary>
/// <param name="shiftRepository">Loads the open orders that match the filter, with their group links.</param>
/// <param name="orderSealingService">Checks the sealing requirements and performs the transition per order.</param>
/// <param name="mediator">Sends the group-assignment command when AutoAssignGroups is requested.</param>
public sealed class SealOpenOrdersCommandHandler
    : IRequestHandler<SealOpenOrdersCommand, SealOpenOrdersResult>
{
    private const int MaxSample = 20;

    private const string AutoAssignOutsideFullAccessError =
        "The group assignment works on orders without any group, which only an administrator with unrestricted " +
        "group scope may touch.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IOrderSealingService _orderSealingService;
    private readonly IMediator _mediator;

    public SealOpenOrdersCommandHandler(
        IShiftRepository shiftRepository,
        IOrderSealingService orderSealingService,
        IMediator mediator)
    {
        _shiftRepository = shiftRepository;
        _orderSealingService = orderSealingService;
        _mediator = mediator;
    }

    public async Task<SealOpenOrdersResult> Handle(
        SealOpenOrdersCommand request, CancellationToken cancellationToken)
    {
        if (request.AutoAssignGroups && !request.Scope.HasFullAccess)
        {
            throw new ForbiddenException(AutoAssignOutsideFullAccessError);
        }

        if (request.Apply && request.ExpectedSelection != null)
        {
            await EnsureStillAsConfirmedAsync(request, request.ExpectedSelection, cancellationToken);
        }

        var autoAssignedCount = 0;
        if (request.AutoAssignGroups)
        {
            var assignment = await _mediator.Send(
                new AssignOrdersToGroupsCommand(
                    request.SourceSystemId,
                    request.FromDate,
                    request.UntilDate,
                    request.CustomerName,
                    request.MaxCount,
                    request.ValidFrom,
                    request.Apply,
                    request.UserName),
                cancellationToken);

            autoAssignedCount = assignment.AssignedCount;
        }

        var orders = await LoadOrdersInScopeAsync(request, cancellationToken);
        var expected = request.ExpectedSelection;
        if (expected != null)
        {
            orders = orders.Where(order => expected.Contains(order.Id)).ToList();
        }

        var (sealable, blocked) = Classify(orders);

        if (!request.Apply)
        {
            return BuildResult(request, orders.Count, sealable, [], blocked, [], autoAssignedCount);
        }

        var sealedOrders = new List<SealedOrderInfo>();
        var failures = new List<FailedOrder>();

        foreach (var order in sealable)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var outcome = await _orderSealingService.SealAsync(order.Id, cancellationToken);
                if (outcome.IsSealed)
                {
                    sealedOrders.Add(new SealedOrderInfo(order.Id, order.Name, outcome.PlannableShiftId!.Value));
                }
                else
                {
                    failures.Add(new FailedOrder(order.Id, order.Name, outcome.FailureReason!));
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                failures.Add(new FailedOrder(order.Id, order.Name, ex.Message));
            }
        }

        return BuildResult(request, orders.Count, sealable, sealedOrders, blocked, failures, autoAssignedCount);
    }

    private async Task EnsureStillAsConfirmedAsync(
        SealOpenOrdersCommand request, OpenOrderSealingSelection confirmed, CancellationToken cancellationToken)
    {
        var (sealable, blocked) = Classify(await LoadOrdersInScopeAsync(request, cancellationToken));
        var current = OpenOrderSealingSelection.Of(sealable.Select(order => order.Id), blocked.Select(order => order.OrderId));
        if (!current.HasSameEntriesAs(confirmed))
        {
            throw new InvalidRequestException(ConfirmationBindingMessages.Describe(
                SealOpenOrdersBinding.Of(confirmed), SealOpenOrdersBinding.Of(current)));
        }
    }

    private (List<Shift> Sealable, List<BlockedOrder> Blocked) Classify(IEnumerable<Shift> orders)
    {
        var sealable = new List<Shift>();
        var blocked = new List<BlockedOrder>();

        foreach (var order in orders)
        {
            var missing = _orderSealingService.CollectMissingRequirements(order);
            if (missing.Count > 0)
            {
                blocked.Add(new BlockedOrder(order.Id, order.Name, missing));
                continue;
            }

            sealable.Add(order);
        }

        return (sealable, blocked);
    }

    private async Task<List<Shift>> LoadOrdersInScopeAsync(
        SealOpenOrdersCommand request, CancellationToken cancellationToken)
    {
        var scope = request.Scope;
        var orders = await _shiftRepository.GetOpenOrdersAsync(
            new OpenOrderFilter(
                request.SourceSystemId,
                request.FromDate,
                request.UntilDate,
                request.CustomerName,
                request.GroupId,
                scope.HasFullAccess ? request.MaxCount : null),
            cancellationToken);

        if (scope.HasFullAccess)
        {
            return orders;
        }

        var ordersInScope = orders.Where(scope.Includes);
        return (request.MaxCount.HasValue ? ordersInScope.Take(request.MaxCount.Value) : ordersInScope).ToList();
    }

    private static SealOpenOrdersResult BuildResult(
        SealOpenOrdersCommand request,
        int totalOrders,
        IReadOnlyList<Shift> sealable,
        IReadOnlyList<SealedOrderInfo> sealedOrders,
        IReadOnlyList<BlockedOrder> blocked,
        IReadOnlyList<FailedOrder> failures,
        int autoAssignedCount) =>
        new(
            Applied: request.Apply,
            TotalOrders: totalOrders,
            SealableCount: sealable.Count,
            SealedCount: sealedOrders.Count,
            BlockedCount: blocked.Count,
            FailedCount: failures.Count,
            BlockedOnlyByMissingGroupCount: blocked.Count(b =>
                b.MissingRequirements.Count == 1
                && b.MissingRequirements[0] == OrderSealingService.MissingGroupRequirement),
            AutoAssignedCount: autoAssignedCount,
            AutoAssignRequested: request.AutoAssignGroups,
            SealedSample: sealedOrders.Take(MaxSample).ToList(),
            BlockedSample: blocked.Take(MaxSample).ToList(),
            Failures: failures.Take(MaxSample).ToList())
        {
            Selection = OpenOrderSealingSelection.Of(sealable.Select(order => order.Id), blocked.Select(order => order.OrderId))
        };
}
