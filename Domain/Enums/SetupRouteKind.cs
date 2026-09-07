// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum SetupRouteKind
{
    ErpImport = 0,
    ErpImportNeedsGroup = 1,
    ErpImportContradictsClientless = 2,
    CustomerOrder = 3,
    CustomerOrderNeedsCustomer = 4,
    CustomerOrderNeedsGroup = 5,
    ClientlessDuty = 6,
    ClientlessDutyNeedsGroup = 7,
    BothRoutesUnclear = 8,
    ShiftsAwaitingAssignment = 9
}
