// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule for which sealed orders count in the billing export: a shift whose client is a customer. A duty without a
/// customer is paid working time nobody is billed for, so it stays out of the order list, the preview and the order file; the
/// wage export reads the works through its own loaders and does not use this rule. A soft-deleted client does not
/// resolve through the query filter, so its orders count as orders without a customer.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.Exports;

internal static class BillableOrderRule
{
    public static readonly Expression<Func<Shift, bool>> HasCustomer =
        shift => shift.ClientId != null && shift.Client!.Type == EntityTypeEnum.Customer;
}
