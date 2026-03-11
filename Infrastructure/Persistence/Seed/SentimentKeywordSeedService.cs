// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seed-Service für mehrsprachige Sentiment-Keywords der 4 Core-Sprachen (de, en, fr, it).
/// Befüllt die sentiment_keyword_sets-Tabelle beim ersten Start, falls noch keine Einträge vorhanden sind.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class SentimentKeywordSeedService
{
    private readonly ISentimentKeywordRepository _repository;
    private readonly ILogger<SentimentKeywordSeedService> _logger;

    private static readonly Dictionary<string, Dictionary<string, List<string>>> CoreKeywords = new()
    {
        ["de"] = new Dictionary<string, List<string>>
        {
            ["frustrated"] = ["funktioniert nicht", "geht nicht", "schon wieder", "warum geht", "warum funktioniert", "hilfe", "problem", "kaputt", "defekt", "falsch", "fehler"],
            ["stressed"] = ["dringend", "sofort", "schnell", "deadline", "eilig", "muss jetzt", "keine zeit", "zeitdruck", "wichtig und dringend"],
            ["confused"] = ["verstehe nicht", "versteh nicht", "wie geht", "wie funktioniert", "was ist", "was bedeutet", "was meinst", "unklar", "nicht klar", "hä", "keine ahnung", "erklär"],
            ["positive"] = ["danke", "vielen dank", "super", "perfekt", "toll", "funktioniert", "klappt", "klasse", "prima", "wunderbar", "ausgezeichnet", "gut gemacht", "genial"],
            ["urgent"] = ["wichtig", "dringend", "sofort", "notfall"]
        },
        ["en"] = new Dictionary<string, List<string>>
        {
            ["frustrated"] = ["doesn't work", "not working", "broken", "again", "still not", "still broken", "error", "bug", "wtf", "ugh", "argh", "seriously", "ridiculous"],
            ["stressed"] = ["urgent", "immediately", "right now", "hurry", "asap", "as soon as possible", "need this now", "critical", "no time"],
            ["confused"] = ["confused", "don't understand", "what does", "what is", "how do i", "how does", "i don't get", "i'm lost", "no idea", "explain", "unclear"],
            ["positive"] = ["thank you", "thanks", "great", "perfect", "awesome", "excellent", "works", "worked", "amazing", "fantastic", "love it"],
            ["urgent"] = ["important", "urgent", "emergency", "critical", "immediately", "right now", "asap"]
        },
        ["fr"] = new Dictionary<string, List<string>>
        {
            ["frustrated"] = ["ne fonctionne pas", "ne marche pas", "encore une fois", "pourquoi", "cassé", "problème", "erreur", "bug", "défaut", "panne"],
            ["stressed"] = ["urgent", "immédiatement", "tout de suite", "vite", "délai", "pressé", "pas le temps", "dépêche"],
            ["confused"] = ["je ne comprends pas", "comment", "qu'est-ce que", "c'est quoi", "pas clair", "expliquer", "aucune idée", "perdu"],
            ["positive"] = ["merci", "parfait", "super", "génial", "excellent", "formidable", "bravo", "bien joué", "magnifique"],
            ["urgent"] = ["important", "urgent", "urgence", "critique", "immédiatement"]
        },
        ["it"] = new Dictionary<string, List<string>>
        {
            ["frustrated"] = ["non funziona", "non va", "di nuovo", "perché", "rotto", "problema", "errore", "bug", "guasto", "difetto"],
            ["stressed"] = ["urgente", "subito", "immediatamente", "fretta", "scadenza", "non ho tempo", "pressione"],
            ["confused"] = ["non capisco", "come funziona", "cos'è", "che significa", "non chiaro", "spiegare", "nessuna idea", "perso"],
            ["positive"] = ["grazie", "perfetto", "fantastico", "ottimo", "eccellente", "bravo", "meraviglioso", "magnifico"],
            ["urgent"] = ["importante", "urgente", "emergenza", "critico", "subito"]
        }
    };

    public SentimentKeywordSeedService(
        ISentimentKeywordRepository repository,
        ILogger<SentimentKeywordSeedService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetAllAsync(cancellationToken);
        if (existing.Count > 0)
        {
            _logger.LogInformation("Sentiment keyword sets already exist ({Count} languages). Skipping seed.", existing.Count);
            return;
        }

        foreach (var (language, keywords) in CoreKeywords)
        {
            await _repository.UpsertAsync(language, keywords, SentimentKeywordSources.Seed, cancellationToken);
        }

        _logger.LogInformation("Seeded sentiment keywords for {Count} core language(s).", CoreKeywords.Count);
    }
}
