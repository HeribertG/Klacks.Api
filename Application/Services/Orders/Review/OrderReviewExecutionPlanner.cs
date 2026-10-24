// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Plans execute_order_review from the review draft of the conversation: the ready orders of the target (at most
/// OrderReviewDefaults.MaxOrdersPerExecution), each loaded as it is now and refused when it is gone, hidden or no longer
/// an open order. A start change changes the order itself, so it needs acknowledgedOrderChange=true and the role
/// Admin or Authorised (supervisor); giving an order its first group needs an administrator with unrestricted group
/// scope. The text is the shared execution text plus the default expenses an order already carries (every part carries
/// them too). Reads only.
/// </summary>
/// <param name="draftStore">Holds the review draft of the conversation.</param>
/// <param name="loader">Resolves the caller's review scope.</param>
/// <param name="shiftRepository">Loads each order with its groups, expenses and qualifications.</param>
/// <param name="shiftVisibilityGuard">Answers an order the caller may not see like a missing one.</param>

using System.Globalization;
using System.Text;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.OrderReview;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Orders.Review;

public sealed class OrderReviewExecutionPlanner : IOrderReviewExecutionPlanner
{
    public const string TargetParameter = "target";
    public const string AcknowledgedOrderChangeParameter = "acknowledgedOrderChange";

    private const string NoReviewError = "No order review in progress. Start one with start_order_review first.";
    private const string NothingReadyFormat =
        "No order of this target is ready to be executed. Still open: {0}. Ask for these first, then preview again.";
    private const string StartChangeNotAcknowledged =
        "This execution changes the start of at least one order, which changes the order itself. Show the preview with " +
        "its section 'Changes to orders' and call execute_order_review with acknowledgedOrderChange=true only after the " +
        "user confirmed that change. Nothing was changed.";
    private const string StartChangeNeedsRole =
        "Changing the start of an order is reserved to administrators and supervisors. Keep the start (startTime equal to " +
        "the current start) or ask an administrator. Nothing was changed.";
    private const string GroupNeedsFullAccess =
        "Giving an order its first group is reserved to an administrator with unrestricted group scope. Nothing was changed.";
    private const string OrderChangedFormat =
        "Order '{0}' is no longer an open order you can see (sealed, deleted or moved out of your scope since the review " +
        "started). Leave it out with set_order_review_decision skip=true or start the review again. Nothing was changed.";
    private const string ExistingExpensesFormat = "'{0}' already carries default expenses {1}; every part carries them as well.";
    private const string ExpenseSeparator = ", ";
    private const string TaxableMarker = " (taxable)";

    private readonly IPendingOrderReviewDraftStore _draftStore;
    private readonly IOrderReviewLoader _loader;
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public OrderReviewExecutionPlanner(
        IPendingOrderReviewDraftStore draftStore,
        IOrderReviewLoader loader,
        IShiftRepository shiftRepository,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _draftStore = draftStore;
        _loader = loader;
        _shiftRepository = shiftRepository;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<OrderReviewExecutionPlan> PlanAsync(
        SkillExecutionContext context, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        var draft = _draftStore.Get(context.UserId, OrderReviewDraftScope.ConversationKey(context));
        if (draft is null)
        {
            return OrderReviewExecutionPlan.Refuse(NoReviewError);
        }

        var (targets, targetError) = OrderReviewTargets.Resolve(draft, SkillParameterReader.Read<string>(parameters, TargetParameter));
        if (targetError != null)
        {
            return OrderReviewExecutionPlan.Refuse(targetError);
        }

        var (ready, moreReady, notReady) = OrderReviewExecutionText.SelectReady(targets);
        if (ready.Count == 0)
        {
            var open = notReady.SelectMany(OrderReviewReadiness.OpenAspects).Distinct().ToList();
            return OrderReviewExecutionPlan.Refuse(string.Format(NothingReadyFormat, string.Join(ExpenseSeparator, open)));
        }

        var rightsRefusal = await CheckRightsAsync(context, parameters, ready, cancellationToken);
        if (rightsRefusal != null)
        {
            return OrderReviewExecutionPlan.Refuse(rightsRefusal);
        }

        var loaded = new List<OrderReviewExecutionTarget>();
        foreach (var item in ready)
        {
            var order = await _shiftRepository.Get(item.OrderId);
            if (order == null || order.Status != ShiftStatus.OriginalOrder
                || !await _shiftVisibilityGuard.IsVisibleAsync(order, cancellationToken))
            {
                return OrderReviewExecutionPlan.Refuse(string.Format(OrderChangedFormat, item.OrderName));
            }

            loaded.Add(new OrderReviewExecutionTarget(item, order));
        }

        return new OrderReviewExecutionPlan(
            null, draft, loaded, moreReady, notReady.Count, OrderReviewBinding.Of(loaded), Text(loaded, moreReady));
    }

    private async Task<string?> CheckRightsAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        IReadOnlyList<OrderReviewDraftItem> ready,
        CancellationToken cancellationToken)
    {
        if (ready.Any(OrderReviewReadiness.ChangesStart))
        {
            if (SkillParameterReader.Read<bool?>(parameters, AcknowledgedOrderChangeParameter) != true)
            {
                return StartChangeNotAcknowledged;
            }

            if (!context.UserPermissions.Contains(Roles.Admin) && !context.UserPermissions.Contains(Roles.Authorised))
            {
                return StartChangeNeedsRole;
            }
        }

        if (ready.Any(item => !item.HasGroup))
        {
            var scope = await _loader.ResolveScopeAsync(context, cancellationToken);
            if (!scope.HasFullAccess)
            {
                return GroupNeedsFullAccess;
            }
        }

        return null;
    }

    private static string Text(IReadOnlyList<OrderReviewExecutionTarget> targets, int moreReady)
    {
        var text = new StringBuilder(OrderReviewExecutionText.Describe(targets.Select(target => target.Item).ToList(), moreReady));
        foreach (var target in targets.Where(target => target.Order.ShiftExpenses.Any(expense => !expense.IsDeleted)))
        {
            var existing = string.Join(ExpenseSeparator, target.Order.ShiftExpenses
                .Where(expense => !expense.IsDeleted)
                .Select(expense => expense.Description + " " + expense.Amount.ToString(CultureInfo.InvariantCulture)
                    + (expense.Taxable ? TaxableMarker : string.Empty)));
            text.AppendLine().Append(string.Format(CultureInfo.InvariantCulture, ExistingExpensesFormat, target.Item.OrderName, existing));
        }

        return text.ToString();
    }
}
