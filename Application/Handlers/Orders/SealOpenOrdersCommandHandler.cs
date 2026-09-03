// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Orders;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Orders;

/// <summary>
/// Handler for <see cref="SealOpenOrdersCommand"/>. Sealing runs through <see cref="IOrderSealingService"/>,
/// the same path seal_shift uses, once per order — that service opens the transaction, so the loop must not
/// wrap one around it and every order commits or rolls back on its own. A refusal or exception is recorded
/// against that order and the batch continues. The optional group assignment is sent as its own command
/// before the loop, never inside it, because it opens a transaction of its own as well.
/// </summary>
/// <param name="shiftRepository">Loads the open orders that match the filter, with their group links.</param>
/// <param name="orderSealingService">Checks the sealing requirements and performs the transition per order.</param>
/// <param name="mediator">Sends the group-assignment command when AutoAssignGroups is requested.</param>
public sealed class SealOpenOrdersCommandHandler
    : IRequestHandler<SealOpenOrdersCommand, SealOpenOrdersResult>
{
    private const int MaxSample = 20;

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

        var orders = await _shiftRepository.GetOpenOrdersAsync(
            new OpenOrderFilter(
                request.SourceSystemId,
                request.FromDate,
                request.UntilDate,
                request.CustomerName,
                request.GroupId,
                request.MaxCount),
            cancellationToken);

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

        if (!request.Apply)
        {
            return BuildResult(request, orders.Count, sealable.Count, [], blocked, [], autoAssignedCount);
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

        return BuildResult(request, orders.Count, sealable.Count, sealedOrders, blocked, failures, autoAssignedCount);
    }

    private static SealOpenOrdersResult BuildResult(
        SealOpenOrdersCommand request,
        int totalOrders,
        int sealableCount,
        IReadOnlyList<SealedOrderInfo> sealedOrders,
        IReadOnlyList<BlockedOrder> blocked,
        IReadOnlyList<FailedOrder> failures,
        int autoAssignedCount) =>
        new(
            Applied: request.Apply,
            TotalOrders: totalOrders,
            SealableCount: sealableCount,
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
            Failures: failures.Take(MaxSample).ToList());
}
