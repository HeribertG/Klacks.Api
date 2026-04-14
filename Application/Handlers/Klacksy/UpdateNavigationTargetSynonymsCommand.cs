// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command and handler for updating synonyms for a navigation target, writing to the core manifest or a locale overlay file.
/// </summary>
/// <param name="TargetId">The unique identifier of the navigation target to update</param>
/// <param name="Locale">The locale whose synonyms are being updated (e.g. "de", "en", "fr")</param>
/// <param name="Synonyms">The new set of synonyms for this locale</param>
/// <param name="Status">The new synonym status (e.g. "generated", "approved")</param>
namespace Klacks.Api.Application.Handlers.Klacksy;

using System.Text.Json;
using Klacks.Api.Application.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Infrastructure.Mediator;

public record UpdateNavigationTargetSynonymsCommand(string TargetId, string Locale, string[] Synonyms, string Status) : IRequest<bool>;

public sealed class UpdateNavigationTargetSynonymsCommandHandler : IRequestHandler<UpdateNavigationTargetSynonymsCommand, bool>
{
    private static readonly string[] CoreLocales = ["de", "en", "fr", "it"];

    private readonly INavigationTargetCacheService _cache;

    public UpdateNavigationTargetSynonymsCommandHandler(INavigationTargetCacheService cache) => _cache = cache;

    public Task<bool> Handle(UpdateNavigationTargetSynonymsCommand command, CancellationToken cancellationToken)
    {
        var baseDir = AppContext.BaseDirectory;

        if (CoreLocales.Contains(command.Locale))
        {
            var manifest = Path.Combine(baseDir, "Application", "Skills", "Definitions", "navigation-targets.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            var list = JsonSerializer.Deserialize<List<NavigationTarget>>(File.ReadAllText(manifest))!;
            var idx = list.FindIndex(x => x.TargetId == command.TargetId);
            if (idx < 0) throw new KeyNotFoundException(command.TargetId);
            list[idx].Synonyms[command.Locale] = command.Synonyms;
            File.WriteAllText(manifest, JsonSerializer.Serialize(list, options));
        }
        else
        {
            var file = Path.Combine(baseDir, "Plugins", "Languages", command.Locale, "navigation-targets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var overlay = File.Exists(file)
                ? JsonSerializer.Deserialize<Dictionary<string, PluginEntry>>(File.ReadAllText(file))!
                : new Dictionary<string, PluginEntry>();
            overlay[command.TargetId] = new PluginEntry { Synonyms = command.Synonyms, Status = command.Status };
            File.WriteAllText(file, JsonSerializer.Serialize(overlay, new JsonSerializerOptions { WriteIndented = true }));
        }

        _cache.Invalidate();
        return Task.FromResult(true);
    }

    private sealed class PluginEntry
    {
        public string[] Synonyms { get; set; } = [];
        public string Status { get; set; } = "generated";
    }
}
