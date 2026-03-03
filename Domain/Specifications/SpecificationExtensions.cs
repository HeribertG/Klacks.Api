// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Specifications;

public static class SpecificationExtensions
{
    public static IQueryable<T> Where<T>(this IQueryable<T> query, ISpecification<T> specification)
    {
        return query.Where(specification.ToExpression());
    }

    public static IEnumerable<T> Where<T>(this IEnumerable<T> collection, ISpecification<T> specification)
    {
        return collection.Where(specification.ToExpression().Compile());
    }
}
