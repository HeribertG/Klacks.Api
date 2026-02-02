using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Domain.Services.Skills.Implementations;

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
    {
        if (!parameters.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        if (value is T typedValue)
        {
            return typedValue;
        }

        try
        {
            if (typeof(T) == typeof(string))
            {
                return (T)(object)value.ToString()!;
            }

            if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
            {
                return (T)(object)Convert.ToInt32(value);
            }

            if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
            {
                return (T)(object)Convert.ToBoolean(value);
            }

            if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
            {
                return (T)(object)Guid.Parse(value.ToString()!);
            }

            if (typeof(T) == typeof(DateOnly) || typeof(T) == typeof(DateOnly?))
            {
                return (T)(object)DateOnly.Parse(value.ToString()!);
            }

            if (typeof(T) == typeof(TimeOnly) || typeof(T) == typeof(TimeOnly?))
            {
                return (T)(object)TimeOnly.Parse(value.ToString()!);
            }

            if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
            {
                return (T)(object)DateTime.Parse(value.ToString()!);
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

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
