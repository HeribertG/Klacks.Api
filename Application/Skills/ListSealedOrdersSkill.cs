// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists sealed orders (Shifts with Status=SealedOrder) that are eligible for the order export,
/// including customer information and Work-closing statistics, via ListSealedOrdersQuery.
/// An order is ready for export when all of its works are closed.
/// </summary>
/// <param name="fromDate">Optional. ISO date yyyy-MM-dd, lower bound of the order active window.</param>
/// <param name="untilDate">Optional. ISO date yyyy-MM-dd, upper bound of the order active window.</param>
/// <param name="customerId">Optional. UUID of the customer the orders belong to.</param>
/// <param name="search">Optional. Case-insensitive search across abbreviation, name and customer.</param>

using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_sealed_orders")]
public class ListSealedOrdersSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListSealedOrdersSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");
        var customerId = GetParameter<Guid?>(parameters, "customerId");
        var search = GetParameter<string>(parameters, "search");

        if (fromDate.HasValue && untilDate.HasValue && fromDate.Value > untilDate.Value)
        {
            return SkillResult.Error("fromDate must not be after untilDate.");
        }

        var orders = await _mediator.Send(
            new ListSealedOrdersQuery(fromDate, untilDate, customerId, search), cancellationToken);

        var projected = orders
            .Select(o => new
            {
                o.Id,
                o.Abbreviation,
                o.Name,
                o.FromDate,
                o.UntilDate,
                o.CustomerName,
                o.CustomerNumber,
                o.TotalWorks,
                o.ClosedWorks,
                ReadyForExport = o.TotalWorks > 0 && o.ClosedWorks == o.TotalWorks
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, Orders = projected },
            $"Found {projected.Count} sealed order(s).");
    }
}
