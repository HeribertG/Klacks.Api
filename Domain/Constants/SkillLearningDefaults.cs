// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fallback values for the learning loop, used whenever the matching settings key is absent or unparsable.
/// The thresholds start deliberately low because the loop only learns once people actually talk to the
/// assistant; they are settings-backed so they can be raised with real traffic without a deploy.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningDefaults
{
    public const int MinOccurrences = 3;
    public const int MinDistinctUsers = 2;
    public const int PruneDays = 30;
    public const int RetentionDays = 90;
    public const bool ReportOptIn = false;

    /// <summary>
    /// Maximum length of the stored excerpt of a user utterance. Nothing longer than this is ever
    /// persisted by the learning loop - the full message never leaves the turn.
    /// </summary>
    public const int ExcerptMaxLength = 120;

    /// <summary>
    /// Shortest utterance that may open a case at all. Below this the refusal phrases match noise
    /// ("nein", "was?") far more often than a real capability wish.
    /// </summary>
    public const int MinTokenCount = 3;

    public const int ToolsetCandidatesMax = 30;

    public const int MaxLearningAttempts = 2;

    /// <summary>
    /// How long a claimed cluster may stay in Learning before another instance may take it over, so a
    /// process that died mid-round does not park the cluster forever.
    /// </summary>
    public const int StaleClaimMinutes = 60;

    /// <summary>
    /// How many ready clusters one learning run may take on. The cap bounds both the language model cost
    /// of a run and the number of index rebuilds it triggers, because every activated phrase forces a
    /// catalogue refresh before the routing oracle can judge it.
    /// </summary>
    public const int MaxClustersPerRun = 5;

    /// <summary>
    /// Phrase variants the generator produces per round. Three is enough to cover the usual spread of
    /// wordings without turning one cluster into a dozen index rebuilds.
    /// </summary>
    public const int PhraseVariantsPerRound = 3;

    /// <summary>
    /// K of the routing oracle: how many tools the assembled toolset may hold while the target still
    /// counts as "found". Mirrors the retrieval stage's DefaultTopK rather than the provider tool cap,
    /// so a phrase that only wins a guarantee slot is not mistaken for a retrieval success.
    /// </summary>
    public const int RoutingProbeTopK = 20;

    /// <summary>
    /// Shortest and longest a generated phrase may be. The lower bound keeps single particles out of the
    /// index, the upper bound is the excerpt limit - a "phrase" longer than the utterance it should match
    /// is a sentence, and sentences dilute the skill's embedding text.
    /// </summary>
    public const int MinPhraseLength = 3;

    public const int MaxPhraseLength = 120;

    /// <summary>
    /// Window inside which the same user producing the same signal for the same cluster counts once. The
    /// refusal path and the implicit-correction path see the same unhappy turn from two sides, so without
    /// this window a single failed exchange would push a cluster over the repetition threshold on its own.
    /// </summary>
    public const int DedupWindowMinutes = 60;

    /// <summary>
    /// Upper bound on golden cases replayed per regression check. The check runs an embedding and a
    /// reranking pass per case, so an unbounded goldset would make every learning round scale with the
    /// full history of everything ever learned.
    /// </summary>
    public const int MaxGoldenCasesPerRegressionCheck = 200;

    /// <summary>
    /// How many existing phrases of the target skill are shown to the generator as context, so it does
    /// not propose a wording that is already indexed.
    /// </summary>
    public const int GeneratorExistingPhraseSamples = 5;

    /// <summary>
    /// How many cases of a cluster are read to resolve the corrected target skill and the offered toolset.
    /// </summary>
    public const int ClusterCaseSampleSize = 20;

    /// <summary>
    /// Who a proposal was reviewed by when the loop applied or blocked it. A literal rather than a user
    /// id, because no person made the decision and the card must be able to say so.
    /// </summary>
    public const string AutomaticReviewer = "klacksy-learning";

    /// <summary>
    /// How many pending description proposals one run may decide on. Each decision costs a catalogue
    /// refresh plus a full replay of the goldset, twice when it has to be rolled back.
    /// </summary>
    public const int MaxProposalsPerRun = 3;
}
