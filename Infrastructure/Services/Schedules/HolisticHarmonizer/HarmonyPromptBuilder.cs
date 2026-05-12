// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/**
 * Static helpers for building LLM system prompt and user message strings for the Holistic Harmonizer.
 * Constructs structured prompts including candidate moves, prior rejections, and intent-focused goals.
 */

using System.Globalization;
using System.Text;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Candidates;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

internal static class HarmonyPromptBuilder
{
    internal static string BuildSystemPrompt(PlanProposalRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a deterministic schedule-harmonizer assistant. Unlike a per-cell optimizer,");
        sb.AppendLine("you take a HOLISTIC view of the plan: you may group several coordinated swaps into a");
        sb.AppendLine("single batch when they only make sense together. The host system applies a batch as");
        sb.AppendLine("ONE atomic transformation — intermediate steps may temporarily worsen the score, but");
        sb.AppendLine("the final state must be at least as good. Otherwise the whole batch is reverted.");
        sb.AppendLine();
        sb.AppendLine("ALLOWED INTENTS (use the exact label):");
        sb.AppendLine("- consolidate_block — merge fragmented work blocks of one employee into a longer");
        sb.AppendLine("  contiguous run by moving the gap-filling shifts in coordination with neighbours.");
        sb.AppendLine("- enlarge_pause — widen the rest period between two work blocks of one employee");
        sb.AppendLine("  by shifting an edge work day onto a colleague who has more slack.");
        sb.AppendLine("- redistribute_load — move a shift from an over-target employee onto an under-target");
        sb.AppendLine("  colleague so both move closer to their contractual hours.");
        sb.AppendLine();
        sb.AppendLine("OUTPUT CONTRACT (mandatory):");
        sb.AppendLine("- Reply with ONE JSON object and nothing else.");
        sb.AppendLine("- No prose, no markdown, no code fences, no commentary before or after.");
        sb.AppendLine("- Schema:");
        sb.AppendLine("  {\"batches\": [");
        sb.AppendLine("    {\"intent\": \"consolidate_block\",");
        sb.AppendLine("     \"steps\": [ {\"rowA\":int,\"dayA\":int,\"rowB\":int,\"dayB\":int,\"reason\":string}, ... ] },");
        sb.AppendLine("    ... up to 3 batches per response ...");
        sb.AppendLine("  ]}");
        sb.AppendLine("- Each batch may contain at most " + request.MaxStepsPerBatch + " steps (current adaptive cap).");
        sb.AppendLine("- Each step's reason MUST be in language: " + request.Language + ".");
        sb.AppendLine("- If no improvement is possible, reply with {\"batches\": []}.");
        sb.AppendLine();
        sb.AppendLine("HARD CONSTRAINTS (any step violating these is rejected automatically):");
        sb.AppendLine("- Never swap a cell whose symbol is followed by an asterisk (*) — the asterisk marks LOCKED cells.");
        sb.AppendLine("- Never swap a B cell — B is a break/absence and is always locked.");
        sb.AppendLine("- Same-day swaps (DayA == DayB) preserve daily coverage automatically.");
        sb.AppendLine("- Cross-day swaps (DayA != DayB) are allowed ONLY when both cells share the same work-or-free state:");
        sb.AppendLine("    • both cells are work shifts (E/L/N/O) — daily head-counts on each affected day stay equal");
        sb.AppendLine("    • OR both cells are blank/Free — but that swap has zero effect, so do not propose it");
        sb.AppendLine("  Mixing work with blank across two different days breaks daily coverage and is rejected.");
        sb.AppendLine("- (RowA, DayA) must differ from (RowB, DayB) — at least one of the two must change.");
        sb.AppendLine();
        sb.AppendLine("BATCH SEMANTICS:");
        sb.AppendLine("- Steps are applied in order. If step k is rejected, the host keeps the longest valid prefix.");
        sb.AppendLine("- The prefix is committed only if its end-state score does not regress.");
        sb.AppendLine("- Group only steps that genuinely belong together (one consolidate intent = one batch).");
        sb.AppendLine();
        sb.AppendLine("COORDINATE SYSTEM:");
        sb.AppendLine("- rowA / rowB: zero-based row index from the r## prefix on each schedule row.");
        sb.AppendLine("- dayA / dayB: zero-based day index — first column after the row label = day 0, next = day 1, ...");
        sb.AppendLine();
        sb.AppendLine("VISUAL INPUT (when an image is attached):");
        sb.AppendLine("- The image renders the same schedule as a grid: rows are agents (top-down, agent initials in the left header), columns are days (day number + weekday letter in the top header).");
        sb.AppendLine("- Each non-Free cell carries a SINGLE LETTER inside it identifying the shift type:");
        sb.AppendLine("    E = Early (yellow background)");
        sb.AppendLine("    L = Late (orange background)");
        sb.AppendLine("    N = Night (dark-blue background, white letter)");
        sb.AppendLine("    O = Other (grey background, white letter)");
        sb.AppendLine("    B = Break (red/white diagonal stripes, always locked)");
        sb.AppendLine("- Free cells are blank/white with no letter — these are the gap candidates for consolidate_block.");
        sb.AppendLine("- A thick black border around a cell marks it as LOCKED (must NOT be swapped).");
        sb.AppendLine("- Light beige column tint marks Saturday and Sunday.");
        sb.AppendLine("- IMPORTANT: two cells carrying the SAME letter contain the same shift type — swapping them has zero effect. Do NOT propose such swaps.");
        sb.AppendLine("- Read coordinates from the row index (r##) and zero-based day index (first column = day 0).");
        sb.AppendLine();
        if (request.CandidateMoves is not null && request.CandidateMoves.Count > 0)
        {
            sb.AppendLine("CANDIDATE MOVES (a list is attached below in the user message):");
            sb.AppendLine("The host has pre-computed a list of structurally promising swaps for the focused");
            sb.AppendLine("intent. Every listed candidate has already passed the hard-constraint validator");
            sb.AppendLine("(locks, bounds, max-consecutive, min-pause, coverage).");
            sb.AppendLine();
            sb.AppendLine("STRICT MODE: the host runtime DROPS any step whose (rowA,dayA,rowB,dayB) tuple is");
            sb.AppendLine("not present in the candidate list — original coordinates are SILENTLY DISCARDED");
            sb.AppendLine("before evaluation when a candidate list is supplied. To make your steps actually");
            sb.AppendLine("count this iteration:");
            sb.AppendLine("- Pick coordinates ONLY from the candidate list. Mirroring (swapping rowA<->rowB");
            sb.AppendLine("  and dayA<->dayB) is fine, both orientations match.");
            sb.AppendLine("- Combine multiple candidates into one batch when their effects compose (e.g. two");
            sb.AppendLine("  consolidate-block swaps that together fill a longer gap).");
            sb.AppendLine("- If no candidate fits, reply with empty steps[] for this intent — that is more");
            sb.AppendLine("  useful to the search than a self-proposed step that will be dropped.");
            sb.AppendLine();
        }
        sb.AppendLine("PRE-SUBMISSION SELF-CHECK (MANDATORY for every step):");
        sb.AppendLine("Before adding a step to the JSON output, explicitly verify in your reasoning that the");
        sb.AppendLine("letter at (rowA, dayA) is DIFFERENT from the letter at (rowB, dayB). A blank cell counts");
        sb.AppendLine("as a distinct symbol from any letter, but two letters that match (e.g. both L, both E)");
        sb.AppendLine("must NOT be swapped — such steps will be rejected as zero-effect by the host validator");
        sb.AppendLine("and waste the iteration budget. If you cannot confirm a difference, drop the step.");
        sb.AppendLine();
        sb.AppendLine("BAD PATTERN — DO NOT EMIT (these are auto-rejected as zero-effect):");
        sb.AppendLine("  rowA=2 dayA=5 (cell shows 'L')   ↔   rowB=7 dayB=5 (cell shows 'L')   — both Late, useless");
        sb.AppendLine("  rowA=4 dayA=3 (cell shows 'E')   ↔   rowB=8 dayB=3 (cell shows 'E')   — both Early, useless");
        sb.AppendLine("  rowA=1 dayA=0 (blank)            ↔   rowB=9 dayB=0 (blank)            — both Free, useless");
        sb.AppendLine();
        sb.AppendLine("GOOD PATTERN — emit only swaps where the letters / blank-state truly differ, e.g.:");
        sb.AppendLine("  rowA=3 dayA=4 (blank Free)       ↔   rowB=6 dayB=4 (cell shows 'E')   — fills a gap, useful");
        sb.AppendLine("  rowA=0 dayA=2 (cell shows 'L')   ↔   rowB=5 dayB=2 (cell shows 'N')   — redistributes load");
        sb.AppendLine();
        sb.AppendLine("If you are uncertain whether two cells display the same letter, prefer dropping the step.");
        sb.AppendLine("It is always cheaper to skip a doubtful swap than to spend an iteration on a no-op.");
        return sb.ToString();
    }

    internal static string BuildUserMessage(PlanProposalRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ITERATION: " + request.IterationIndex + " (zero-based)");
        sb.AppendLine();
        sb.AppendLine("SCHEDULE GRID:");
        sb.AppendLine(request.PlanText);
        sb.AppendLine();
        sb.AppendLine(request.FragmentationSummary);
        sb.AppendLine();
        sb.AppendLine("AGENT CONSTRAINTS:");
        sb.AppendLine(request.AgentSummary);
        sb.AppendLine();
        AppendPriorRejections(sb, request.PriorRejections);
        AppendCandidateMoves(sb, request.CandidateMoves, request.FocusedIntent);
        AppendFocusedGoal(sb, request.FocusedIntent);
        sb.AppendLine();
        sb.AppendLine("Now reply with the JSON object as defined in the system prompt. No other text.");
        return sb.ToString();
    }

    private static void AppendCandidateMoves(
        StringBuilder sb,
        IReadOnlyList<MoveCandidate>? candidates,
        string focusedIntent)
    {
        if (candidates is null || candidates.Count == 0)
        {
            return;
        }

        sb.AppendLine("########################################################################");
        sb.AppendLine("# CANDIDATE MOVES — pre-validated by the host                          #");
        sb.AppendLine("# Each row below has already passed the hard-constraint validator. Pick #");
        sb.AppendLine("# from this list whenever possible; the rejection risk is much lower    #");
        sb.AppendLine("# than for original coordinates. You may combine candidates into one    #");
        sb.AppendLine("# batch when their effects compose.                                     #");
        sb.AppendLine("########################################################################");
        sb.Append("FOCUSED INTENT: ");
        sb.AppendLine(focusedIntent);
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            sb.AppendFormat(
                CultureInfo.InvariantCulture,
                "  C{0:D2}: rowA={1} dayA={2} <-> rowB={3} dayB={4}  ({5})",
                i + 1, c.RowA, c.DayA, c.RowB, c.DayB, c.Hint);
            sb.AppendLine();
        }
        sb.AppendLine();
    }

    private static void AppendFocusedGoal(StringBuilder sb, string focusedIntent)
    {
        switch (focusedIntent)
        {
            case HolisticIntent.EnlargePause:
                sb.AppendLine("FOCUS THIS ITERATION — enlarge_pause:");
                sb.AppendLine("Identify employees whose two work blocks are separated by only a short rest (1-2 free days).");
                sb.AppendLine("Propose a coordinated batch that widens that pause by swapping the work day adjacent to the");
                sb.AppendLine("gap onto a colleague with more slack. The target employee ends up with a longer rest");
                sb.AppendLine("period; daily coverage stays intact because DayA == DayB. You may still emit other intents");
                sb.AppendLine("if you spot a better opportunity, but prefer enlarge_pause this round.");
                break;
            case HolisticIntent.RedistributeLoad:
                sb.AppendLine("FOCUS THIS ITERATION — redistribute_load:");
                sb.AppendLine("Identify two employees whose actual hours are far from target — one over (too many shifts),");
                sb.AppendLine("one under (too few). Propose a coordinated batch that moves a single work day from the");
                sb.AppendLine("over-target row onto the under-target row on the same day. Both rows shift toward target.");
                sb.AppendLine("Use the AGENT CONSTRAINTS section to read each row's target hours. You may still emit");
                sb.AppendLine("other intents if you spot a better opportunity, but prefer redistribute_load this round.");
                break;
            case HolisticIntent.ConsolidateBlock:
            default:
                sb.AppendLine("FOCUS THIS ITERATION — consolidate_block:");
                sb.AppendLine("Identify employees whose work days are fragmented (e.g. Mon-Tue _ Thu-Fri leaves a Wed gap).");
                sb.AppendLine("Propose a coordinated batch that fills the gap by swapping shifts with other employees on the");
                sb.AppendLine("gap days, so the target employee gets a longer contiguous block. The neighbouring employees");
                sb.AppendLine("must remain within their constraints — DayA == DayB protects daily coverage automatically.");
                break;
        }
    }

    private static void AppendPriorRejections(StringBuilder sb, IReadOnlyList<RejectMemoryEntry> rejections)
    {
        if (rejections.Count == 0)
        {
            return;
        }

        var forbiddenKeys = CollectForbiddenSwapKeys(rejections);
        if (forbiddenKeys.Count > 0)
        {
            sb.AppendLine("########################################################################");
            sb.AppendLine("# CRITICAL — FORBIDDEN COORDINATE PAIRS                                #");
            sb.AppendLine("# The host has REJECTED the swaps below in earlier iterations of THIS  #");
            sb.AppendLine("# run. Re-emitting any of them wastes the iteration budget and is auto-#");
            sb.AppendLine("# rejected before evaluation. Reject any thought of reusing them BEFORE #");
            sb.AppendLine("# you write the JSON output.                                            #");
            sb.AppendLine("########################################################################");
            for (var i = 0; i < forbiddenKeys.Count; i++)
            {
                var key = forbiddenKeys[i];
                sb.AppendLine($"  FORBIDDEN: row {key.RowSmaller} <-> row {key.RowLarger} on day {key.Day}");
            }
            sb.AppendLine();
            sb.AppendLine("Self-check before emitting each step: scan the FORBIDDEN list above and");
            sb.AppendLine("confirm the (rowA, rowB, dayA) triple is not present. If it is, drop the step.");
            sb.AppendLine();
        }

        sb.AppendLine("PRIOR REJECTION REASONS (learn from why earlier batches failed):");
        for (var i = 0; i < rejections.Count; i++)
        {
            var entry = rejections[i];
            sb.AppendLine($"- [{entry.Intent}/{entry.Result}] {entry.Summary}");
        }
        sb.AppendLine();
    }

    private static IReadOnlyList<ForbiddenSwapKey> CollectForbiddenSwapKeys(IReadOnlyList<RejectMemoryEntry> rejections)
    {
        var seen = new HashSet<ForbiddenSwapKey>();
        var ordered = new List<ForbiddenSwapKey>();
        for (var i = 0; i < rejections.Count; i++)
        {
            var swaps = rejections[i].RejectedSwaps;
            for (var j = 0; j < swaps.Count; j++)
            {
                var key = ForbiddenSwapKey.From(swaps[j]);
                if (seen.Add(key))
                {
                    ordered.Add(key);
                }
            }
        }
        return ordered;
    }
}
