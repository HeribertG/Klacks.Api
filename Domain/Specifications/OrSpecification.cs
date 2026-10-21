// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Specification that is satisfied when at least one of the wrapped specifications is satisfied.
/// </summary>
/// <param name="left">First specification to combine</param>
/// <param name="right">Second specification to combine</param>
using System.Linq.Expressions;

namespace Klacks.Api.Domain.Specifications;

internal sealed class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var leftExpr = _left.ToExpression();
        var rightExpr = _right.ToExpression();

        var parameter = Expression.Parameter(typeof(T));
        var combined = Expression.OrElse(
            Expression.Invoke(leftExpr, parameter),
            Expression.Invoke(rightExpr, parameter));

        return Expression.Lambda<Func<T, bool>>(combined, parameter);
    }
}
