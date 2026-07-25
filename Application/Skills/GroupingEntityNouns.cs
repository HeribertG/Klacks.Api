// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps an EntityTypeEnum value to the singular noun used in propose_grouping/apply_grouping result
/// messages (e.g. "employee(s) moved").
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Skills;

public static class GroupingEntityNouns
{
    public static string Noun(EntityTypeEnum entityType) => entityType switch
    {
        EntityTypeEnum.Employee => "employee",
        EntityTypeEnum.ExternEmp => "external employee",
        EntityTypeEnum.Customer => "customer",
        _ => "client"
    };
}
