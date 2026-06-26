// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Base class for skill implementations that provides parameter extraction helpers.
/// Metadata (name, description, parameters) comes from the database via SkillDescriptor.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

public abstract class BaseSkillImplementation : ISkillImplementation
{
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

        if (value is JsonElement jsonElement)
        {
            value = UnwrapJsonElement(jsonElement);
        }

        if (value is null)
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

            if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
            {
                return (T)(object)Convert.ToDecimal(value);
            }

            if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
            {
                return (T)(object)Convert.ToDouble(value);
            }

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    private static object? UnwrapJsonElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.GetRawText()
        };
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

    protected static decimal CalculateWorkTime(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(24));
        }

        return (decimal)duration.TotalHours;
    }

    /// <summary>
    /// Re-reads a just-written entity straight from the database (no tracking cache) and throws a
    /// <see cref="SkillVerificationException"/> when it is missing or fails the validity check. Intended
    /// to run inside <c>IUnitOfWork.ExecuteInTransactionAsync</c> after the write, so a failed check
    /// aborts the operation and rolls the write back instead of reporting a success that never persisted.
    /// </summary>
    /// <param name="skillName">Name of the calling skill, carried on the exception for diagnostics.</param>
    /// <param name="reread">Fresh database read of the written entity (e.g. repository GetNoTracking).</param>
    /// <param name="isPersisted">Predicate confirming the read entity matches the intended write.</param>
    /// <param name="description">Human-readable description of what was being persisted.</param>
    protected static async Task ConfirmPersistedAsync<TEntity>(
        string skillName,
        Func<Task<TEntity?>> reread,
        Func<TEntity, bool> isPersisted,
        string description)
        where TEntity : class
    {
        var persisted = await reread();
        if (persisted is null || !isPersisted(persisted))
        {
            throw new SkillVerificationException(
                skillName,
                $"Database verification failed: {description} could not be confirmed in the database " +
                "after the write — the change was rolled back.");
        }
    }
}
