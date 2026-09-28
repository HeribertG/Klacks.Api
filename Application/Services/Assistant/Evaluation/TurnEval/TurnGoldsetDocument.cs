// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Root document of a turn-selection goldset JSON file.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnGoldsetDocument
{
    public int Version { get; set; }

    public string? Kind { get; set; }

    public List<TurnGoldsetItem> Items { get; set; } = new();
}
