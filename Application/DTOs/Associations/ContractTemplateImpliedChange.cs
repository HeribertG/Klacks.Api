// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A change that creating a contract from a template makes on top of the administrator's wishes, because
/// switching the workload path resets the values that belong to the other path (fixed guaranteed hours drop
/// when a percent is wished, the percent drops when fixed hours are wished, and the minimum/maximum band is
/// reset or collapses to the new guaranteed hours). It is not a wish mismatch and does not count as a
/// difference; it is reported so the proposal can name it before anything is created.
/// </summary>
/// <param name="Field">Contract field name as the contract skills expose it</param>
/// <param name="TemplateValue">The template's current value, rendered for display</param>
/// <param name="ResultingValue">The value the new contract gets, rendered for display</param>
namespace Klacks.Api.Application.DTOs.Associations;

public sealed record ContractTemplateImpliedChange(string Field, string TemplateValue, string ResultingValue);
