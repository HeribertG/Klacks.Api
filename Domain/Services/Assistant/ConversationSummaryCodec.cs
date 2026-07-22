// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parses, sanitizes, length-bounds and renders conversation summaries stored in
/// LLMConversation.Summary. A summary is either structured JSON (StructuredConversationSummary)
/// or legacy free text; every method degrades gracefully to the free-text path.
/// </summary>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ConversationSummaryCodec
{
    private const int MaxItemsPerSection = 12;
    private const int MaxEntryLength = 500;

    private const string OpenTasksHeader = "Open tasks:";
    private const string EntitiesHeader = "Entities:";
    private const string DecisionsHeader = "Decisions:";
    private const string FactsHeader = "Facts:";
    private const string Bullet = "- ";

    private static readonly JsonSerializerOptions ParseOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions SerializeOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static bool TryParse(string? raw, out StructuredConversationSummary summary)
    {
        summary = new StructuredConversationSummary();

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var json = ExtractJsonObject(raw);
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        StructuredConversationSummary? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<StructuredConversationSummary>(json, ParseOptions);
        }
        catch (JsonException)
        {
            return false;
        }

        if (parsed == null)
        {
            return false;
        }

        var cleaned = Clean(parsed);
        if (!HasContent(cleaned))
        {
            return false;
        }

        summary = cleaned;
        return true;
    }

    public static string Serialize(StructuredConversationSummary summary)
    {
        return JsonSerializer.Serialize(summary, SerializeOptions);
    }

    public static string Fit(StructuredConversationSummary summary, int maxSerializedLength)
    {
        var working = Clean(summary);
        var serialized = Serialize(working);

        while (serialized.Length > maxSerializedLength && DropOneLeastImportant(working))
        {
            serialized = Serialize(working);
        }

        return serialized;
    }

    public static string? RenderInner(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return TryParse(raw, out var structured) ? RenderStructured(structured) : raw;
    }

    private static string RenderStructured(StructuredConversationSummary summary)
    {
        var sb = new StringBuilder();

        AppendStringSection(sb, OpenTasksHeader, summary.OpenTasks);
        AppendEntitySection(sb, EntitiesHeader, summary.TouchedEntities);
        AppendStringSection(sb, DecisionsHeader, summary.Decisions);
        AppendStringSection(sb, FactsHeader, summary.Facts);

        return sb.ToString().TrimEnd();
    }

    private static void AppendStringSection(StringBuilder sb, string header, List<string> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        sb.AppendLine(header);
        foreach (var item in items)
        {
            sb.Append(Bullet).AppendLine(item);
        }
    }

    private static void AppendEntitySection(StringBuilder sb, string header, List<TouchedEntity> entities)
    {
        if (entities.Count == 0)
        {
            return;
        }

        sb.AppendLine(header);
        foreach (var entity in entities)
        {
            sb.Append(Bullet).AppendLine(RenderEntity(entity));
        }
    }

    private static string RenderEntity(TouchedEntity entity)
    {
        var label = $"{entity.Type} {entity.Name}".Trim();
        return string.IsNullOrWhiteSpace(entity.Id) ? label : $"{label} ({entity.Id})";
    }

    private static StructuredConversationSummary Clean(StructuredConversationSummary summary)
    {
        return new StructuredConversationSummary
        {
            OpenTasks = CleanStrings(summary.OpenTasks),
            TouchedEntities = CleanEntities(summary.TouchedEntities),
            Decisions = CleanStrings(summary.Decisions),
            Facts = CleanStrings(summary.Facts)
        };
    }

    private static List<string> CleanStrings(List<string>? items)
    {
        if (items == null)
        {
            return new List<string>();
        }

        return items
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => CapLength(s.Trim()))
            .Take(MaxItemsPerSection)
            .ToList();
    }

    private static List<TouchedEntity> CleanEntities(List<TouchedEntity>? entities)
    {
        if (entities == null)
        {
            return new List<TouchedEntity>();
        }

        return entities
            .Where(e => e != null
                && (!string.IsNullOrWhiteSpace(e.Type) || !string.IsNullOrWhiteSpace(e.Name)))
            .Select(e => new TouchedEntity
            {
                Type = CapLength((e.Type ?? string.Empty).Trim()),
                Name = CapLength((e.Name ?? string.Empty).Trim()),
                Id = string.IsNullOrWhiteSpace(e.Id) ? null : CapLength(e.Id.Trim())
            })
            .Take(MaxItemsPerSection)
            .ToList();
    }

    private static string CapLength(string value)
    {
        return value.Length <= MaxEntryLength ? value : value[..MaxEntryLength];
    }

    private static bool HasContent(StructuredConversationSummary summary)
    {
        return summary.OpenTasks.Count > 0
            || summary.TouchedEntities.Count > 0
            || summary.Decisions.Count > 0
            || summary.Facts.Count > 0;
    }

    private static bool DropOneLeastImportant(StructuredConversationSummary summary)
    {
        if (RemoveLast(summary.Facts))
        {
            return true;
        }

        if (RemoveLast(summary.Decisions))
        {
            return true;
        }

        if (summary.TouchedEntities.Count > 0)
        {
            summary.TouchedEntities.RemoveAt(summary.TouchedEntities.Count - 1);
            return true;
        }

        return RemoveLast(summary.OpenTasks);
    }

    private static bool RemoveLast(List<string> items)
    {
        if (items.Count == 0)
        {
            return false;
        }

        items.RemoveAt(items.Count - 1);
        return true;
    }

    private static string ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');

        if (start < 0 || end < 0 || end <= start)
        {
            return string.Empty;
        }

        return content[start..(end + 1)];
    }
}
