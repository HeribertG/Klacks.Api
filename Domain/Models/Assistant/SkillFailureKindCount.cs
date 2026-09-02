// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillFailureKindCount(SkillFailureKind Kind, int Count);
