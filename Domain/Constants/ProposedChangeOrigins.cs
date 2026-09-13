// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What produced a proposed skill change. The origin decides which gate judges it: a correction-born
/// proposal is replayed against the holdout golden cases, a goldset-born one against the holdout goldset
/// items of the skills it confuses.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class ProposedChangeOrigins
{
    public const string Correction = "correction";
    public const string GoldsetEval = "goldset_eval";
}
