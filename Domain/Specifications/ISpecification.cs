// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Linq.Expressions;

namespace Klacks.Api.Domain.Specifications;

public interface ISpecification<T>
{
    Expression<Func<T, bool>> ToExpression();
    bool IsSatisfiedBy(T entity);
}
