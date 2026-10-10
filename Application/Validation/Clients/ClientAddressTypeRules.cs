// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Which address types a client may carry depending on its entity type: an employee has only the main
/// address, an external employee additionally a business address and a customer also an invoicing
/// address. Shared by the client POST/PUT validators and the address POST/PUT validators; the Ui mirrors
/// the table in its address type rules constants.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Validation.Clients;

public static class ClientAddressTypeRules
{
    public const string NotAllowedMessage = "address.edit-address.address-persona.validation.address-type-not-allowed";

    private static readonly AddressTypeEnum[] EmployeeTypes =
    {
        AddressTypeEnum.Employee,
    };

    private static readonly AddressTypeEnum[] ExternEmpTypes =
    {
        AddressTypeEnum.Employee,
        AddressTypeEnum.Workplace,
    };

    private static readonly AddressTypeEnum[] CustomerTypes =
    {
        AddressTypeEnum.Employee,
        AddressTypeEnum.Workplace,
        AddressTypeEnum.InvoicingAddress,
    };

    public static IReadOnlyList<AddressTypeEnum> AllowedTypes(EntityTypeEnum clientType) => clientType switch
    {
        EntityTypeEnum.Employee => EmployeeTypes,
        EntityTypeEnum.ExternEmp => ExternEmpTypes,
        EntityTypeEnum.Customer => CustomerTypes,
        _ => CustomerTypes,
    };

    public static bool IsAllowed(EntityTypeEnum clientType, AddressTypeEnum addressType) =>
        AllowedTypes(clientType).Contains(addressType);

    public static string DescribeViolation(EntityTypeEnum clientType, AddressTypeEnum addressType) =>
        $"Address type {addressType} is not allowed for a client of type {clientType}. " +
        $"Allowed address types: {string.Join(", ", AllowedTypes(clientType))}.";
}
