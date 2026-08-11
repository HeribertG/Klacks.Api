// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

public abstract class BaseSkill : ISkill
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract SkillCategory Category { get; }
    public abstract IReadOnlyList<SkillParameter> Parameters { get; }

    public virtual IReadOnlyList<string> RequiredPermissions => Array.Empty<string>();
    public virtual IReadOnlyList<LLMCapability> RequiredCapabilities => Array.Empty<LLMCapability>();

    public abstract Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    protected static T? GetParameter<T>(Dictionary<string, object> parameters, string name, T? defaultValue = default)
        => SkillParameterReader.Read(parameters, name, defaultValue);

    protected static string GetRequiredString(Dictionary<string, object> parameters, string name)
    {
        return GetParameter<string>(parameters, name)
               ?? throw new ArgumentException($"Required parameter '{name}' is missing");
    }

    protected static int GetRequiredInt(Dictionary<string, object> parameters, string name)
    {
        return GetParameter<int?>(parameters, name)
               ?? throw new ArgumentException($"Required parameter '{name}' is missing");
    }

    protected static Guid GetRequiredGuid(Dictionary<string, object> parameters, string name)
    {
        var value = GetParameter<string>(parameters, name);
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException($"Required parameter '{name}' is missing");
        }

        return Guid.Parse(value);
    }
}
