// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for the <see cref="Klacks.Api.Domain.Models.Settings.CompanyRule"/> registry:
/// list the active (non-deleted) rules, load a single rule by id, resolve a rule by its display name for
/// the revert flow, add a newly applied rule and soft-delete a rule on revert.
/// </summary>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICompanyRuleRepository
{
    Task<List<CompanyRule>> GetAllActiveAsync();

    Task<CompanyRule?> GetAsync(Guid id);

    void Add(CompanyRule rule);

    Task<CompanyRule?> DeleteAsync(Guid id);
}
