// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Picks the setup route from the two classified answers and what the installation already holds.
/// Pure and static so the matrix is testable without a database: the caller supplies the snapshot,
/// this decides. Two rules run ahead of the matrix — an installation that already has shifts needs
/// assignments rather than advice, and an unknown attribution never leads to a create offer, only to
/// an explanation of both routes.
/// </summary>
/// <param name="state">What the installation already holds.</param>
/// <param name="attribution">Classified answer to "are the hours attributed to a customer".</param>
/// <param name="orderSource">Classified answer to "do orders come from an outside system".</param>
/// <param name="isAdmin">Whether the asking user may create a clientless duty.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class SetupRouteResolver
{
    private const string OrderListTarget = "shift-list";
    private const string NewShiftTarget = "new-shift";
    private const string NewPlannableShiftTarget = "new-plannable-shift";
    private const string GroupListTarget = "group-list";
    private const string NewEmployeeTarget = "new-employee";
    private const string ErpDropPointsTarget = "erp-drop-points";
    private const string ScheduleTarget = "schedule";

    private static readonly IReadOnlyList<string> NothingMissing = [];

    public static SetupRouteFacts Resolve(
        ScheduleSetupState state,
        SetupAttributionAnswer attribution,
        SetupOrderSourceAnswer orderSource,
        bool isAdmin)
    {
        if (state.HasShifts)
        {
            return new SetupRouteFacts(
                SetupRouteKind.ShiftsAwaitingAssignment, ScheduleTarget, NothingMissing, null, false);
        }

        if (orderSource == SetupOrderSourceAnswer.External)
        {
            return ResolveErpRoute(state, attribution);
        }

        return attribution switch
        {
            SetupAttributionAnswer.Customer => ResolveCustomerRoute(state),
            SetupAttributionAnswer.None => ResolveClientlessRoute(state, isAdmin),
            _ => ResolveUnclearRoute(state)
        };
    }

    private static SetupRouteFacts ResolveErpRoute(ScheduleSetupState state, SetupAttributionAnswer attribution)
    {
        if (attribution == SetupAttributionAnswer.None)
        {
            return new SetupRouteFacts(
                SetupRouteKind.ErpImportContradictsClientless, ErpDropPointsTarget, NothingMissing, null, false);
        }

        return state.HasGroups
            ? new SetupRouteFacts(SetupRouteKind.ErpImport, ErpDropPointsTarget, NothingMissing, null, false)
            : new SetupRouteFacts(
                SetupRouteKind.ErpImportNeedsGroup,
                GroupListTarget,
                [SetupPrerequisites.Group],
                SetupHandoffPhrases.CreateGroup,
                false);
    }

    private static SetupRouteFacts ResolveCustomerRoute(ScheduleSetupState state)
    {
        if (!state.HasCustomers)
        {
            return new SetupRouteFacts(
                SetupRouteKind.CustomerOrderNeedsCustomer,
                NewEmployeeTarget,
                [SetupPrerequisites.Customer],
                null,
                false);
        }

        if (!state.HasGroups)
        {
            return new SetupRouteFacts(
                SetupRouteKind.CustomerOrderNeedsGroup,
                GroupListTarget,
                [SetupPrerequisites.Group],
                SetupHandoffPhrases.CreateGroup,
                false);
        }

        return new SetupRouteFacts(
            SetupRouteKind.CustomerOrder,
            NewShiftTarget,
            NothingMissing,
            SetupHandoffPhrases.CreateShiftOrder,
            false);
    }

    private static SetupRouteFacts ResolveClientlessRoute(ScheduleSetupState state, bool isAdmin)
    {
        if (!state.HasGroups)
        {
            return new SetupRouteFacts(
                SetupRouteKind.ClientlessDutyNeedsGroup,
                GroupListTarget,
                [SetupPrerequisites.Group],
                SetupHandoffPhrases.CreateGroup,
                false);
        }

        IReadOnlyList<string> missing = isAdmin ? NothingMissing : [SetupPrerequisites.AdminRights];

        return new SetupRouteFacts(
            SetupRouteKind.ClientlessDuty, NewPlannableShiftTarget, missing, null, true);
    }

    private static SetupRouteFacts ResolveUnclearRoute(ScheduleSetupState state)
    {
        var target = state.HasCustomers ? NewShiftTarget : OrderListTarget;
        IReadOnlyList<string> missing = state.HasGroups ? NothingMissing : [SetupPrerequisites.Group];

        return new SetupRouteFacts(SetupRouteKind.BothRoutesUnclear, target, missing, null, false);
    }
}
