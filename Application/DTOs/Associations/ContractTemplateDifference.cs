// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One field in which a contract template does not match the administrator's wish.
/// </summary>
/// <param name="Field">Contract field name as the contract skills expose it</param>
/// <param name="TemplateValue">The template's current value, rendered for display</param>
/// <param name="WishedValue">The value the administrator wished for, rendered for display</param>
namespace Klacks.Api.Application.DTOs.Associations;

public sealed record ContractTemplateDifference(string Field, string TemplateValue, string WishedValue);
