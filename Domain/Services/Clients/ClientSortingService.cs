// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientSortingService : IClientSortingService
{
    public IQueryable<Client> ApplySorting(IQueryable<Client> query, string? orderBy, string? sortOrder)
    {
        if (string.IsNullOrEmpty(orderBy))
        {
            return query.OrderBy(c => c.Name).ThenBy(c => c.FirstName);
        }

        var isDescending = !string.IsNullOrEmpty(sortOrder) && sortOrder.ToLower() == "desc";

        return orderBy.ToLower() switch
        {
            "name" => isDescending 
                ? query.OrderByDescending(c => c.Name).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.Name).ThenBy(c => c.FirstName),
            
            "firstname" => isDescending 
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.Name),
            
            "company" => isDescending 
                ? query.OrderByDescending(c => c.Company).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.Company).ThenBy(c => c.Name),
            
            "idnumber" => isDescending 
                ? query.OrderByDescending(c => c.IdNumber)
                : query.OrderBy(c => c.IdNumber),
            
            "createtime" => isDescending 
                ? query.OrderByDescending(c => c.CreateTime)
                : query.OrderBy(c => c.CreateTime),
            
            "updatetime" => isDescending 
                ? query.OrderByDescending(c => c.UpdateTime)
                : query.OrderBy(c => c.UpdateTime),
            
            _ => query.OrderBy(c => c.Name).ThenBy(c => c.FirstName)
        };
    }
}