// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Where an order takes place: the customer's workplace address (Einsatzort) wins, the main address (Employee) is the
/// fallback, the invoicing address never counts. Within a type the newest address (ValidFrom) wins. Shared by the
/// group assignment of orders (OrderGroupPlanner, ShiftCityGroupPlanner: Select with the criterion they need, e.g.
/// "has a city") and the order location filter of the shift list (LocatedAt: the first address that carries any
/// place data - city, zip or state - must match the criteria). The group membership of an order is a planning and
/// visibility instrument and says nothing about this location.
/// </summary>
/// <param name="addresses">The customer's addresses</param>
/// <param name="isUsable">Criterion an address must meet to be considered (e.g. has a city, has coordinates)</param>
/// <param name="criteria">Resolved location criteria the order location must match</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Geo;

namespace Klacks.Api.Domain.Services.Shifts;

public static class OrderLocationAddressRule
{
    public const AddressTypeEnum PreferredType = AddressTypeEnum.Workplace;
    public const AddressTypeEnum FallbackType = AddressTypeEnum.Employee;

    private static readonly Expression<Func<Shift, IEnumerable<Address>>> LocationAddress = s => s.Client!.Addresses
        .Where(a => !a.IsDeleted
            && (a.Type == PreferredType || a.Type == FallbackType)
            && (a.City.Trim() != string.Empty || a.Zip.Trim() != string.Empty || a.State.Trim() != string.Empty))
        .OrderByDescending(a => a.Type == PreferredType)
        .ThenByDescending(a => a.ValidFrom)
        .Take(1);

    public static Address? Select(IEnumerable<Address> addresses, Func<Address, bool> isUsable)
    {
        var usable = addresses.Where(a => !a.IsDeleted && isUsable(a)).ToList();
        return NewestOfType(usable, PreferredType) ?? NewestOfType(usable, FallbackType);
    }

    public static bool HasPlaceData(Address address)
        => !string.IsNullOrWhiteSpace(address.City)
           || !string.IsNullOrWhiteSpace(address.Zip)
           || !string.IsNullOrWhiteSpace(address.State);

    public static Expression<Func<Shift, bool>> LocatedAt(AddressLocationCriteria criteria)
    {
        var located = AddressLocationPredicate.AnyOf(LocationAddress, criteria);
        var order = located.Parameters[0];
        var hasCustomer = Expression.NotEqual(
            Expression.Property(order, nameof(Shift.Client)), Expression.Constant(null, typeof(Client)));
        return Expression.Lambda<Func<Shift, bool>>(Expression.AndAlso(hasCustomer, located.Body), order);
    }

    private static Address? NewestOfType(IEnumerable<Address> addresses, AddressTypeEnum type)
        => addresses
            .Where(a => a.Type == type)
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault();
}
