// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// See ISkillRerankThreshold. A singleton: one threshold per process, read on every skill pass and written by the
/// tuner. A double is read and written atomically on 64-bit runtimes; Volatile keeps the latest value visible to
/// every thread without a lock on the hot path.
/// </summary>
/// <param name="options">The configured mode, starting threshold and exploration rate</param>

using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public sealed class SkillRerankThresholdState : ISkillRerankThreshold
{
    private double _current;

    public SkillRerankThresholdState(SkillRerankOptions options)
    {
        IsLearned = options.Mode == SkillRerankMode.Adaptive;
        ExplorationRate = Math.Clamp(options.ExplorationRate, 0, KnowledgeIndexConstants.MaxSkillRerankExplorationRate);
        _current = Clamp(options.MarginThreshold);
    }

    public bool IsLearned { get; }

    public double ExplorationRate { get; }

    public double Current => Volatile.Read(ref _current);

    public void Set(double value) => Volatile.Write(ref _current, Clamp(value));

    private static double Clamp(double value) =>
        double.IsFinite(value)
            ? Math.Clamp(
                value,
                KnowledgeIndexConstants.MinSkillRerankMarginThreshold,
                KnowledgeIndexConstants.MaxSkillRerankMarginThreshold)
            : KnowledgeIndexConstants.DefaultSkillRerankMarginThreshold;
}
