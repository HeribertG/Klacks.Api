// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillCallStat(string SkillName, int Calls, int Failures);
