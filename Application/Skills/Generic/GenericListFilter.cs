// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Skills.Generic;

public class GenericListFilter
{
    public string ParameterName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string MatchType { get; set; } = "equals";
}
