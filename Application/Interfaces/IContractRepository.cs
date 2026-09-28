// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Interfaces;

public interface IContractRepository : IBaseRepository<Contract>
{
    Task<int> CountActiveContractsByIndividualPeriodAsync(Guid individualPeriodId);
}