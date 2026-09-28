// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Email;

public class SpamFilterResult
{
    public float Score { get; set; }

    public bool IsSpam { get; set; }

    public string Reason { get; set; } = string.Empty;

    public bool UsedLlm { get; set; }
}
