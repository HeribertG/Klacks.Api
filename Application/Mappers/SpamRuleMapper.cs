// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Models.Email;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class SpamRuleMapper
{
    public partial SpamRuleResource ToResource(SpamRule rule);

    public partial List<SpamRuleResource> ToResources(List<SpamRule> rules);
}
