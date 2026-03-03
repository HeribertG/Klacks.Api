// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class SkillParameterTypeMapping
{
    private static readonly Dictionary<SkillParameterType, string> TypeMap = new()
    {
        { SkillParameterType.String, "string" },
        { SkillParameterType.Integer, "integer" },
        { SkillParameterType.Decimal, "number" },
        { SkillParameterType.Boolean, "boolean" },
        { SkillParameterType.Date, "string" },
        { SkillParameterType.Time, "string" },
        { SkillParameterType.DateTime, "string" },
        { SkillParameterType.Array, "array" },
        { SkillParameterType.Object, "object" },
        { SkillParameterType.Enum, "string" }
    };

    public static string ToJsonSchemaType(SkillParameterType type) =>
        TypeMap.GetValueOrDefault(type, "string");
}
