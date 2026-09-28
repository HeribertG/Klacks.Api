// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Specifications.Shifts;

public sealed class ShiftByClientSpecification : Specification<Shift>
{
    private readonly Guid _clientId;

    public ShiftByClientSpecification(Guid clientId)
    {
        _clientId = clientId;
    }

    public override Expression<Func<Shift, bool>> ToExpression()
    {
        return shift => shift.ClientId == _clientId;
    }
}
