// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Linq.Expressions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Specifications.Shifts;

public sealed class ShiftByStatusSpecification : Specification<Shift>
{
    private readonly ShiftStatus _status;

    public ShiftByStatusSpecification(ShiftStatus status)
    {
        _status = status;
    }

    public override Expression<Func<Shift, bool>> ToExpression()
    {
        return shift => shift.Status == _status;
    }
}
