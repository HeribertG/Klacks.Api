using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Specifications.Shifts;

public sealed class ShiftByDateRangeSpecification : Specification<Shift>
{
    private readonly DateOnly _fromDate;
    private readonly DateOnly _untilDate;

    public ShiftByDateRangeSpecification(DateOnly fromDate, DateOnly untilDate)
    {
        _fromDate = fromDate;
        _untilDate = untilDate;
    }

    public override Expression<Func<Shift, bool>> ToExpression()
    {
        return shift => shift.FromDate >= _fromDate &&
                        (shift.UntilDate == null || shift.UntilDate <= _untilDate);
    }
}
