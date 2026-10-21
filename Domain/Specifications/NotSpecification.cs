// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Specification that negates the wrapped specification.
/// </summary>
/// <param name="specification">Specification to negate</param>
using System.Linq.Expressions;

namespace Klacks.Api.Domain.Specifications;

internal sealed class NotSpecification<T> : Specification<T>
{
    private readonly Specification<T> _specification;

    public NotSpecification(Specification<T> specification)
    {
        _specification = specification;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        var expr = _specification.ToExpression();
        var parameter = Expression.Parameter(typeof(T));
        var negated = Expression.Not(Expression.Invoke(expr, parameter));

        return Expression.Lambda<Func<T, bool>>(negated, parameter);
    }
}
