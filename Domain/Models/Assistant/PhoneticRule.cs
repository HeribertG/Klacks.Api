// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public class PhoneticRule
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}
