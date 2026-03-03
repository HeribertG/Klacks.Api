// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Specifications.Clients;

public sealed class ClientNotDeletedSpecification : Specification<Client>
{
    public override Expression<Func<Client, bool>> ToExpression()
    {
        return client => !client.IsDeleted;
    }
}
