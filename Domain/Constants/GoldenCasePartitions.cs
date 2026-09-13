// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Which half of the goldset a case belongs to. Train cases feed learning and are never replayed by a
/// gate; holdout cases are the only thing any gate is allowed to measure. A learner that could see the
/// cases it is judged on would optimise against its own exam.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class GoldenCasePartitions
{
    public const string Train = "train";
    public const string Holdout = "holdout";
}
