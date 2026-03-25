// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Learns new skill synonyms from successful Tier 2 classifier results.
/// Extracts meaningful words from user messages and adds them as synonyms
/// to matched skills, improving future Tier 1 keyword matching.
/// </summary>
/// <param name="userMessage">Original user message to extract learning words from</param>
/// <param name="matchedSkillNames">Skills that were matched via classifier</param>
/// <param name="language">Current UI language for synonym storage</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public interface ISynonymLearningService
{
    void LearnFromClassifierResult(
        string userMessage,
        List<string> matchedSkillNames,
        string? language,
        Guid agentId);
}

public class SynonymLearningService : ISynonymLearningService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISkillCacheService _skillCacheService;
    private readonly ILogger<SynonymLearningService> _logger;

    private const int MinWordLength = 4;
    private const int MaxNewSynonymsPerSkill = 3;

    private static readonly HashSet<string> StopWordsDe = new(StringComparer.OrdinalIgnoreCase)
    {
        "aber", "alle", "allem", "allen", "aller", "allerdings", "alles", "also",
        "andere", "anderem", "anderen", "anderer", "anderes", "anders", "auch",
        "ausser", "ausserdem", "bald", "bereits", "besonders", "bestimmt",
        "bitte", "bloss", "dabei", "dadurch", "dafür", "dagegen", "daher",
        "dahin", "damals", "damit", "danach", "daneben", "dann", "daran",
        "darauf", "daraus", "darf", "darfst", "darin", "darum", "darunter",
        "davon", "davor", "dazu", "dein", "deine", "deinem", "deinen",
        "deiner", "denn", "dennoch", "deren", "derer", "derjenige", "deshalb",
        "dessen", "dich", "dies", "diese", "dieselbe", "dieselben", "diesem",
        "diesen", "dieser", "dieses", "doch", "dort", "drei", "drin",
        "dritte", "drüben", "dumm", "durch", "dürfen", "ebenfalls", "ebenso",
        "ehe", "eher", "eigentlich", "einige", "einigen", "einiger", "einiges",
        "einmal", "erst", "etwa", "etwas", "euch", "euer", "eure", "eurem",
        "euren", "eurer", "ganz", "gern", "gerne", "gegen", "genau",
        "gerade", "gering", "gewiss", "geworden", "haben", "halb", "halt",
        "hast", "heute", "hier", "hinter", "höchst", "immer", "indem",
        "infolgedessen", "innen", "innerhalb", "jede", "jedem", "jeden",
        "jeder", "jedes", "jedoch", "jene", "jenem", "jenen", "jener",
        "jenes", "jetzt", "kann", "kannst", "kaum", "kein", "keine",
        "keinem", "keinen", "keiner", "könne", "können", "könnt", "konnte",
        "kürzlich", "lange", "längst", "lass", "lässt", "laut", "lediglich",
        "machen", "manche", "manchem", "manchen", "mancher", "manchmal",
        "mehr", "mein", "meine", "meinem", "meinen", "meiner", "meiste",
        "mich", "mindestens", "möchte", "möchten", "mögen", "möglich",
        "morgen", "morgens", "muss", "müssen", "musst", "müsst", "nach",
        "nachdem", "nachher", "nächst", "neben", "nehmen", "nein", "nicht",
        "nichts", "noch", "nochmals", "nunmehr", "oben", "oder", "ohne",
        "recht", "rechts", "schon", "sehr", "seid", "sein", "seine",
        "seinem", "seinen", "seiner", "seit", "seitdem", "selbst", "sich",
        "sicher", "sicherlich", "sind", "sofort", "sogar", "solch", "solche",
        "solchem", "solchen", "solcher", "soll", "sollen", "sollte", "sollten",
        "solltest", "sondern", "sonst", "soviel", "sowie", "über", "überall",
        "überhaupt", "übrigens", "unser", "unsere", "unserem", "unseren",
        "unserer", "unten", "viel", "viele", "vielen", "vielleicht", "voll",
        "völlig", "vom", "vorbei", "vorher", "vorhin", "wahrscheinlich",
        "wann", "warum", "weder", "weil", "weit", "weiter", "weitere",
        "weiterem", "weiteren", "weiterer", "weiteres", "welch", "welche",
        "welchem", "welchen", "welcher", "wenig", "wenige", "wenigen",
        "weniger", "wenigstens", "wenn", "werde", "werden", "werdet",
        "weshalb", "wessen", "wieder", "will", "willst", "wirklich",
        "wirst", "wohl", "wollen", "wollt", "worin", "würde", "würden",
        "ziemlich", "zuerst", "zumal", "zurück", "zusammen", "zwar",
        "zwischen", "eine", "einem", "einen", "einer", "eines", "dass",
        "wird", "wurde", "würde", "hätte", "hätten", "wäre", "waren",
        "gewesen", "könnte", "müsste", "sollte", "dürfte",
        "bisher", "bevor", "falls", "geben", "gibt", "hatte", "häufig",
        "irgend", "jemand", "letzte", "letzten", "lieber", "manche",
        "möchte", "nachdem", "neuen", "nichts", "obwohl", "trotz",
        "trotzdem", "unter", "verschiedene", "während", "welche", "wie",
        "wirklich", "womit", "wozu", "zunächst",
    };

    private static readonly HashSet<string> StopWordsEn = new(StringComparer.OrdinalIgnoreCase)
    {
        "about", "above", "after", "again", "against", "also", "been",
        "before", "being", "below", "between", "both", "could", "does",
        "doing", "down", "during", "each", "every", "from", "further",
        "have", "having", "here", "hers", "herself", "himself", "into",
        "itself", "just", "more", "most", "myself", "once", "only",
        "other", "ourselves", "over", "same", "shall", "should", "some",
        "such", "than", "that", "their", "theirs", "them", "themselves",
        "then", "there", "these", "they", "this", "those", "through",
        "under", "until", "very", "want", "was", "were", "what", "when",
        "where", "which", "while", "whom", "will", "with", "would",
        "your", "yours", "yourself", "yourselves",
    };

    public SynonymLearningService(
        IServiceScopeFactory scopeFactory,
        ISkillCacheService skillCacheService,
        ILogger<SynonymLearningService> logger)
    {
        _scopeFactory = scopeFactory;
        _skillCacheService = skillCacheService;
        _logger = logger;
    }

    public void LearnFromClassifierResult(
        string userMessage,
        List<string> matchedSkillNames,
        string? language,
        Guid agentId)
    {
        if (matchedSkillNames.Count == 0) return;

        var lang = (language ?? "de").ToLowerInvariant();
        var candidateWords = ExtractCandidateWords(userMessage, lang);

        if (candidateWords.Count == 0) return;

        _ = Task.Run(async () =>
        {
            try
            {
                await PersistNewSynonymsAsync(agentId, matchedSkillNames, candidateWords, lang);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to learn synonyms from classifier result");
            }
        });
    }

    private List<string> ExtractCandidateWords(string message, string lang)
    {
        var stopWords = lang == "en" ? StopWordsEn : StopWordsDe;

        var words = message
            .ToLowerInvariant()
            .Split([' ', ',', '.', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '\n', '\r', '\t'],
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= MinWordLength)
            .Where(w => !stopWords.Contains(w))
            .Where(w => w.All(c => char.IsLetter(c) || c == '-'))
            .Distinct()
            .Take(MaxNewSynonymsPerSkill * 2)
            .ToList();

        return words;
    }

    private async Task PersistNewSynonymsAsync(
        Guid agentId,
        List<string> skillNames,
        List<string> candidateWords,
        string lang)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();

        var skills = await repo.GetEnabledAsync(agentId);
        var matchedSkills = skills
            .Where(s => skillNames.Contains(s.Name, StringComparer.OrdinalIgnoreCase))
            .ToList();

        var updated = false;

        foreach (var skill in matchedSkills)
        {
            var synonyms = skill.Synonyms ?? new Dictionary<string, List<string>>();
            if (!synonyms.ContainsKey(lang))
            {
                synonyms[lang] = new List<string>();
            }

            var existingSynonyms = new HashSet<string>(synonyms[lang], StringComparer.OrdinalIgnoreCase);
            var newWords = candidateWords
                .Where(w => !existingSynonyms.Contains(w))
                .Take(MaxNewSynonymsPerSkill)
                .ToList();

            if (newWords.Count == 0) continue;

            synonyms[lang].AddRange(newWords);
            skill.Synonyms = synonyms;
            await repo.UpdateAsync(skill);
            updated = true;

            _logger.LogInformation(
                "Learned {Count} new synonyms for skill {SkillName} [{Lang}]: {Words}",
                newWords.Count, skill.Name, lang, string.Join(", ", newWords));
        }

        if (updated)
        {
            _skillCacheService.InvalidateCache();
        }
    }
}
