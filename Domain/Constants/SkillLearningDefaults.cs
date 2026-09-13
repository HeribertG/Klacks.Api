// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Fallback values for the learning loop, used whenever the matching settings key is absent or unparsable.
/// The thresholds start deliberately low because the loop only learns once people actually talk to the
/// assistant; they are settings-backed so they can be raised with real traffic without a deploy.
/// </summary>

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

    /// <summary>
    /// Width of every skill-name column of the learning loop. The expected skill arrives as free text
    /// from the correction menu, so the value is clipped to this bound before it is stored; the entity
    /// configuration takes its HasMaxLength from here, so the two can no longer drift apart.
    /// </summary>
    public const int SkillNameMaxLength = 128;

    /// <summary>
    /// Maximum length of the optional free-text comment a user may attach to a not-helpful judgement
    /// (W1.8). Stored on the trajectory; longer input is truncated, never rejected.
    /// </summary>
    public const int FeedbackCommentMaxLength = 1000;

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
    /// counts as "found". Mirrors the production ceiling, because the oracle has to judge the world the
    /// user will actually meet.
    /// The earlier value of 20 was meant to keep a phrase that only wins a guarantee slot from counting
    /// as a retrieval success, but it never did that - guaranteed skills are inserted at index 0 and
    /// preferred again when the toolset is truncated. What it did instead was make the probe far
    /// stricter than production: the nine always-on skills survive every truncation, so 20 left eleven
    /// slots for retrieved skills where production leaves twenty-one. A skill ranked twelfth to
    /// twentieth was therefore rejected by the oracle while being offered in production, and the
    /// learner would withdraw a phrase that in fact worked. The concern behind the old value is moot
    /// for learned phrases anyway: they are written to skill_phrase, which reaches only the vector
    /// index, never AgentSkill.Synonyms, so they cannot win a keyword guarantee to begin with.
    /// </summary>
    public const int RoutingProbeTopK = ContextBudgetPolicy.MaxToolsForProviderCeiling;

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
    /// full history of everything ever learned. At roughly one second per probe in development and three
    /// in production, one pass costs about 40 seconds respectively 120 seconds; a sharpening round pays one
    /// baseline pass plus one gated pass per proposal - four passes while the round's proposals share a
    /// skill, one further baseline pass for every additional skill. A goldset-born proposal pays
    /// <see cref="MaxTargetedHoldoutReplaysPerProposal"/> replays on top of that. The old bound of 200 was
    /// never reached by the shipped goldset, so a pass replayed all 83 of its holdout cases and a round
    /// blocked the loop for minutes. The cut is affordable because the budget is no longer spent on
    /// whatever happens to be oldest: the repository fills it with the cases of the skill under change
    /// first, which is the population a widened description is most likely to break.
    /// </summary>
    public const int MaxGoldenCasesPerRegressionCheck = 40;

    /// <summary>
    /// How many holdout golden cases have to exist before the loop may apply a description change on its
    /// own. Below this a green gate does not mean "nothing broke", it means "almost nothing was measured":
    /// with an empty goldset every proposal passed, which is how unmeasured narrowings went live.
    /// Settings-backed via KLACKSY_LEARNING_MIN_GOLDEN_CASES so it can be raised without a deploy.
    /// </summary>
    public const int MinGoldenCasesForAutoApply = 20;

    /// <summary>
    /// Upper bound on holdout goldset items replayed per goldset-born proposal. Each replay is a paid
    /// provider call, so an unbounded targeted gate would make one learning round cost as much as a full
    /// eval run.
    /// </summary>
    public const int MaxTargetedHoldoutReplaysPerProposal = 25;

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

    /// <summary>
    /// How many unconsumed selection misses of the latest full eval run are read per learning run. It is
    /// a safety ceiling, not a sample: the query is already narrowed to the train items of the goldset,
    /// so the value only has to stay above the goldset size, or misses would be cut off by a limit rather
    /// than by the partition and the tail could never be reached.
    /// </summary>
    public const int MaxGoldsetMissesPerRun = 400;

    /// <summary>
    /// How many goldset-born description proposals one run may open. Every group is one paid model call
    /// plus one pending row, and the goldset branch sees hundreds of items where the correction branch
    /// sees the handful of turns a person actually flagged - uncapped it would fill every slot of
    /// <see cref="MaxProposalsPerRun"/> before a correction is ever looked at.
    /// </summary>
    public const int MaxGoldsetProposalsPerRun = 3;

    /// <summary>
    /// How many of the newest pending trigger narrowings are read back to decide whether a cluster already
    /// carries one. A cluster is dismissed in the same pass that opens its proposal, so only a run that died
    /// between the two writes can collide; reading the whole review backlog for every declined recipe would
    /// cost far more than the rare duplicate it prevents.
    /// </summary>
    public const int MaxNarrowingProposalsScannedForDuplicates = 50;

    /// <summary>
    /// Capability variants the generator produces per round, mirroring the phrase budget.
    /// </summary>
    public const int CapabilityVariantsPerRound = 3;

    /// <summary>
    /// Longest step sequence a learned capability may have. A composition nobody can read at a glance is
    /// one an administrator cannot judge in the card either, and every further step multiplies the ways
    /// the chain can misfire on data the probe never saw.
    /// </summary>
    public const int MaxCapabilityStepCount = 4;

    /// <summary>
    /// Shortest trigger stem a learned recipe may use. Below four characters a stem matches unrelated
    /// words at a word boundary far more often than the intent it was meant to catch, which is how a
    /// recipe starts hijacking foreign turns.
    /// </summary>
    public const int MinTriggerStemLength = 4;

    /// <summary>
    /// Prefix every learned recipe name carries. Keeps the learned namespace disjoint from the seeded
    /// slugs, so a later seed definition can never collide with, and therefore never overwrite, a recipe
    /// the loop composed.
    /// </summary>
    public const string LearnedRecipeNamePrefix = "learned-";

    /// <summary>
    /// Sort order given to every learned recipe. Deliberately behind all seeded recipes: the trigger
    /// matcher takes the first match in sort order, so a learned recipe never outranks a hand-written one
    /// even if a disjointness check was ever fooled.
    /// </summary>
    public const int LearnedRecipeSortOrder = 10000;

    /// <summary>
    /// Rolling window in days over which an activated artefact's usefulness is measured, and the idle
    /// period after which an unused one is retired.
    /// </summary>
    public const int FitnessWindowDays = 30;

    /// <summary>
    /// How many observations an artefact needs before a poor quote is allowed to retire it. Below this
    /// a single unlucky turn would decide, and the loop would unlearn faster than it learns.
    /// </summary>
    public const int PruneMinUsesForQuote = 5;

    /// <summary>
    /// The quote at or above which an artefact with enough observations is kept.
    /// </summary>
    public const decimal PruneMinQuote = 0.5m;

    /// <summary>
    /// How many activated artefacts one fitness or pruning pass may look at.
    /// </summary>
    public const int MaxArtefactsPerFitnessRun = 200;
}
