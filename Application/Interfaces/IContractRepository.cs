// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IContractRepository : IBaseRepository<Contract>
{
    Task<int> CountActiveContractsByIndividualPeriodAsync(Guid individualPeriodId);

    /// <summary>
    /// Number of employee assignments of the contract that are still in use: not deleted, of a client that is not
    /// deleted, and either open-ended or ending on or after <paramref name="today"/>. Assignments that ended earlier
    /// do not count, future ones do.
    /// </summary>
    /// <param name="contractId">The contract template.</param>
    /// <param name="today">The company's current calendar day.</param>
    Task<int> CountActiveOrFutureAssignmentsAsync(Guid contractId, DateOnly today);
}