// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Scripting;

public class ResultMessage
{
    public int Type { get; set; }
    public string Message { get; set; } = string.Empty;
}
