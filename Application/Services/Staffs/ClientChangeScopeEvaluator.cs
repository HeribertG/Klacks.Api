// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IClientChangeScopeEvaluator. It re-reads the stored client through the very query the read
/// endpoint uses (IClientRepository.Get, already AsNoTracking, so the update that may follow in the
/// same request keeps an untouched change tracker), maps it with the same mapper the read endpoint
/// uses, and compares the result property by property against the incoming resource. Everything is
/// compared unless it is on the short IgnoredProperties list, so a property added to ClientResource
/// later is guarded automatically rather than silently unguarded. The comparison errs on the side of
/// "changed": a false positive costs a caller one rejected note, a false negative would be a
/// privilege escalation on the core entity.
/// </summary>
/// <param name="clientRepository">Loads the stored client with the relations the read endpoint returns</param>
/// <param name="clientMapper">Maps the stored entity into the resource shape the caller sent</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Staffs;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using System.Collections;
using System.Reflection;

namespace Klacks.Api.Application.Services.Staffs;

public class ClientChangeScopeEvaluator : IClientChangeScopeEvaluator
{
    private const int MaxComparisonDepth = 6;

    private const string ResourceNamespacePrefix = "Klacks.Api.Application.DTOs";

    private const string IdentityPropertyName = "Id";

    /// <summary>
    /// Annotations are the payload a note-only caller is allowed to change. SkipAddressValidation is a
    /// transport flag of the address dialog that is never persisted, so a difference in it is not a
    /// change to the client.
    /// </summary>
    public static readonly IReadOnlyList<string> IgnoredProperties =
    [
        nameof(ClientResource.Annotations),
        nameof(ClientResource.SkipAddressValidation)
    ];

    private readonly IClientRepository _clientRepository;
    private readonly ClientMapper _clientMapper;

    public ClientChangeScopeEvaluator(IClientRepository clientRepository, ClientMapper clientMapper)
    {
        _clientRepository = clientRepository;
        _clientMapper = clientMapper;
    }

    public async Task<ClientChangeScope> EvaluateAsync(ClientResource incoming, CancellationToken cancellationToken = default)
    {
        var stored = await _clientRepository.Get(incoming.Id);
        if (stored == null)
        {
            return ClientChangeScope.NotFound;
        }

        var storedResource = _clientMapper.ToResource(stored);

        foreach (var property in ComparedProperties())
        {
            if (!ValuesMatch(property.GetValue(incoming), property.GetValue(storedResource), 0))
            {
                return ClientChangeScope.BeyondAnnotations;
            }
        }

        return ClientChangeScope.AnnotationsOnly;
    }

    private static IEnumerable<PropertyInfo> ComparedProperties()
        => typeof(ClientResource)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Where(p => !IgnoredProperties.Contains(p.Name, StringComparer.Ordinal));

    private static bool ValuesMatch(object? incoming, object? stored, int depth)
    {
        if (incoming is string || stored is string)
        {
            var left = incoming as string;
            var right = stored as string;
            return string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right)
                || string.Equals(left, right, StringComparison.Ordinal);
        }

        var incomingSequence = incoming as IEnumerable;
        var storedSequence = stored as IEnumerable;
        if (incomingSequence != null || storedSequence != null)
        {
            return SequencesMatch(incomingSequence, storedSequence, depth);
        }

        if (incoming == null || stored == null)
        {
            return incoming == null && stored == null;
        }

        if (incoming is DateTime incomingDate && stored is DateTime storedDate)
        {
            return DateTime.SpecifyKind(incomingDate, DateTimeKind.Unspecified)
                == DateTime.SpecifyKind(storedDate, DateTimeKind.Unspecified);
        }

        var type = incoming.GetType();
        if (type != stored.GetType())
        {
            return false;
        }

        if (IsResourceType(type) && depth < MaxComparisonDepth)
        {
            return NestedResourcesMatch(incoming, stored, type, depth);
        }

        return Equals(incoming, stored);
    }

    private static bool SequencesMatch(IEnumerable? incoming, IEnumerable? stored, int depth)
    {
        var left = Materialise(incoming);
        var right = Materialise(stored);

        if (left.Count != right.Count)
        {
            return false;
        }

        var orderedLeft = OrderByIdentity(left);
        var orderedRight = OrderByIdentity(right);

        for (var i = 0; i < orderedLeft.Count; i++)
        {
            if (!ValuesMatch(orderedLeft[i], orderedRight[i], depth + 1))
            {
                return false;
            }
        }

        return true;
    }

    private static List<object?> Materialise(IEnumerable? items)
    {
        if (items == null)
        {
            return [];
        }

        var list = new List<object?>();
        foreach (var item in items)
        {
            list.Add(item);
        }

        return list;
    }

    private static List<object?> OrderByIdentity(List<object?> items)
    {
        if (items.Any(i => i == null) || items.Count == 0)
        {
            return items;
        }

        var identity = items[0]!.GetType().GetProperty(IdentityPropertyName, BindingFlags.Public | BindingFlags.Instance);
        if (identity == null || identity.PropertyType != typeof(Guid))
        {
            return items;
        }

        return items
            .OrderBy(i => (Guid)(identity.GetValue(i) ?? Guid.Empty))
            .ToList();
    }

    private static bool NestedResourcesMatch(object incoming, object stored, Type type, int depth)
    {
        foreach (var property in type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
        {
            if (!ValuesMatch(property.GetValue(incoming), property.GetValue(stored), depth + 1))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsResourceType(Type type)
        => type.Namespace != null && type.Namespace.StartsWith(ResourceNamespacePrefix, StringComparison.Ordinal);
}
