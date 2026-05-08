// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Loop;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Mutations;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Default implementation of <see cref="IPlanProposalProvider"/>. Bypasses the conversational
/// <c>ILLMService</c> (which mixes Klacks system prompts, conversation history and tool
/// calling into every request) and instead drives the underlying <see cref="ILLMProvider"/>
/// directly so the LLM receives only Holistic Harmonizer's structured prompt and replies with the JSON
/// we expect.
/// </summary>
public sealed partial class LlmPlanProposalProvider : IPlanProposalProvider
{
    private const double ProposalTemperature = 0.2;
    private const int ProposalMaxTokens = 6000;
    private const int ResponsePreviewLength = 120;
    private static readonly TimeSpan ProposalTimeout = TimeSpan.FromSeconds(60);

    private const int PingMaxTokens = 50;
    private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(30);
    private const string PingSystemPrompt =
        "You are a JSON-only test endpoint. Reply with exactly: {\"ping\":\"pong\"}\n" +
        "No prose, no markdown, no commentary, no extra fields.";
    private const string PingUserMessage = "Reply with the JSON object as instructed.";

    private const int CapabilityMaxTokens = 2000;
    private static readonly TimeSpan CapabilityTimeout = TimeSpan.FromSeconds(90);
    private const string CapabilitySystemPrompt =
        "You are a deterministic schedule-harmonizer assistant for a capability self-test.\n" +
        "Reply with ONE JSON object and nothing else: {\"swaps\":[{\"rowA\":int,\"dayA\":int,\"rowB\":int,\"dayB\":int,\"reason\":string}, ...]}\n" +
        "No prose, no markdown, no code fences. Cells marked with * are LOCKED — never swap them.\n" +
        "Constraint: dayA must equal dayB (only same-day swaps).";
    private const string CapabilityUserMessage =
        "Mini schedule (3 employees × 5 days). Symbols: E=Early, L=Late, _=Free, * after symbol = locked.\n" +
        "       d0    d1    d2    d3    d4\n" +
        "r00    L     L     L     L     L\n" +
        "r01    _     _     _     _     _\n" +
        "r02    E     E     E     E     E\n" +
        "\n" +
        "Constraints: r00 max 3 consecutive Late; r01 target 30h; r02 max 3 consecutive Early.\n" +
        "r00 has 5 Late shifts in a row — propose at least one same-day swap that redistributes the load.\n" +
        "Reply with the JSON object only. dayA must equal dayB.";

    private readonly LLMProviderOrchestrator _orchestrator;
    private readonly ILogger<LlmPlanProposalProvider> _logger;

    public LlmPlanProposalProvider(LLMProviderOrchestrator orchestrator, ILogger<LlmPlanProposalProvider> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }

    public async Task<PlanProposalPingResult> PingAsync(string modelId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            stopwatch.Stop();
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, error ?? "LLM provider unavailable.");
        }

        var pingRequest = new LLMProviderRequest
        {
            Message = PingUserMessage,
            SystemPrompt = PingSystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = 0.0,
            MaxTokens = PingMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
        };

        using var pingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pingCts.CancelAfter(PingTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(pingRequest, pingCts.Token);
            stopwatch.Stop();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Ping timed out after {PingTimeout.TotalSeconds:F0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Holistic Harmonizer ping threw for model {ModelId}", modelId);
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Ping failed: {ex.Message}");
        }

        if (!response.Success)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the ping.");
        }

        var content = response.Content ?? string.Empty;
        if (!ContainsPongJson(content))
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Model returned unexpected ping response: {preview}");
        }

        return new PlanProposalPingResult(true, stopwatch.ElapsedMilliseconds, null);
    }

    public async Task<PlanProposalPingResult> CapabilityCheckAsync(string modelId, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(modelId);
        if (error is not null || model is null || provider is null)
        {
            stopwatch.Stop();
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, error ?? "LLM provider unavailable.");
        }

        var capabilityRequest = new LLMProviderRequest
        {
            Message = CapabilityUserMessage,
            SystemPrompt = CapabilitySystemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = 0.2,
            MaxTokens = CapabilityMaxTokens,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
        };

        using var capabilityCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        capabilityCts.CancelAfter(CapabilityTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(capabilityRequest, capabilityCts.Token);
            stopwatch.Stop();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Capability check timed out after {CapabilityTimeout.TotalSeconds:F0}s.");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Holistic Harmonizer capability check threw for model {ModelId}", modelId);
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, $"Capability check failed: {ex.Message}");
        }

        if (!response.Success)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, response.Error ?? "Provider rejected the request.");
        }

        var content = response.Content ?? string.Empty;
        if (string.IsNullOrWhiteSpace(content))
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, "Model returned empty content (likely consumed token budget on internal reasoning).");
        }

        var capabilityError = ValidateCapabilityResponse(content);
        if (capabilityError is not null)
        {
            return new PlanProposalPingResult(false, stopwatch.ElapsedMilliseconds, capabilityError);
        }

        return new PlanProposalPingResult(true, stopwatch.ElapsedMilliseconds, null);
    }

    private static string? ValidateCapabilityResponse(string content)
    {
        var json = ExtractJsonObject(content);
        if (json is null)
        {
            var preview = content.Length > ResponsePreviewLength ? content[..ResponsePreviewLength] + "..." : content;
            return $"No JSON object found. Preview: {preview}";
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("swaps", out var swapsEl) || swapsEl.ValueKind != JsonValueKind.Array)
            {
                return "JSON missing 'swaps' array.";
            }
            if (swapsEl.GetArrayLength() == 0)
            {
                return "Model returned empty 'swaps' array — could not propose any improvement to a clearly-imbalanced mini schedule.";
            }

            foreach (var el in swapsEl.EnumerateArray())
            {
                if (!IsValidCapabilitySwap(el, out var reason))
                {
                    return $"Invalid swap entry: {reason}";
                }
            }
            return null;
        }
        catch (JsonException ex)
        {
            return $"JSON parse failed: {ex.Message}";
        }
    }

    private static bool IsValidCapabilitySwap(JsonElement el, out string reason)
    {
        reason = string.Empty;
        if (el.ValueKind != JsonValueKind.Object)
        {
            reason = "swap is not a JSON object";
            return false;
        }
        if (!el.TryGetProperty("rowA", out var rowAEl) || !rowAEl.TryGetInt32(out var rowA) || rowA < 0 || rowA > 2)
        {
            reason = "rowA missing or out of [0..2]";
            return false;
        }
        if (!el.TryGetProperty("rowB", out var rowBEl) || !rowBEl.TryGetInt32(out var rowB) || rowB < 0 || rowB > 2)
        {
            reason = "rowB missing or out of [0..2]";
            return false;
        }
        if (rowA == rowB)
        {
            reason = "rowA equals rowB";
            return false;
        }
        if (!el.TryGetProperty("dayA", out var dayAEl) || !dayAEl.TryGetInt32(out var dayA) || dayA < 0 || dayA > 4)
        {
            reason = "dayA missing or out of [0..4]";
            return false;
        }
        if (!el.TryGetProperty("dayB", out var dayBEl) || !dayBEl.TryGetInt32(out var dayB) || dayB < 0 || dayB > 4)
        {
            reason = "dayB missing or out of [0..4]";
            return false;
        }
        if (dayA != dayB)
        {
            reason = $"dayA ({dayA}) != dayB ({dayB}) — only same-day swaps allowed";
            return false;
        }
        return true;
    }

    private static bool ContainsPongJson(string content)
    {
        var json = ExtractJsonObject(content);
        if (json is null)
        {
            return false;
        }
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("ping", out var pingEl)
                && pingEl.ValueKind == JsonValueKind.String
                && string.Equals(pingEl.GetString(), "pong", StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task<PlanProposalResponse> ProposeAsync(PlanProposalRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var (model, provider, error) = await _orchestrator.GetModelAndProviderAsync(request.ModelId);
        if (error is not null || model is null || provider is null)
        {
            return new PlanProposalResponse([], string.Empty, error ?? "LLM provider unavailable.");
        }

        var providerRequest = new LLMProviderRequest
        {
            Message = BuildUserMessage(request),
            SystemPrompt = BuildSystemPrompt(request),
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = ProposalTemperature,
            MaxTokens = Math.Min(model.MaxTokens, ProposalMaxTokens),
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
            ImagePng = request.PlanPng,
        };

        using var proposalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        proposalCts.CancelAfter(ProposalTimeout);

        LLMProviderResponse response;
        try
        {
            response = await provider.ProcessAsync(providerRequest, proposalCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Holistic Harmonizer LLM call timed out after {Timeout}s for model {ModelId}",
                ProposalTimeout.TotalSeconds, request.ModelId);
            return new PlanProposalResponse(
                [],
                string.Empty,
                $"LLM call timed out after {ProposalTimeout.TotalSeconds:F0}s — provider did not respond within the per-call budget.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Holistic Harmonizer LLM call threw for model {ModelId}", request.ModelId);
            return new PlanProposalResponse([], string.Empty, $"LLM call failed: {ex.Message}");
        }

        if (!response.Success)
        {
            _logger.LogWarning("Holistic Harmonizer LLM provider returned error for model {ModelId}: {Error}", request.ModelId, response.Error);
            return new PlanProposalResponse([], response.Content ?? string.Empty, response.Error ?? "LLM provider error.");
        }

        var raw = response.Content ?? string.Empty;
        var parsed = TryParseBatches(raw, request.MaxStepsPerBatch, request.IterationIndex, out var parseError);

        _logger.LogInformation(
            "Holistic Harmonizer LLM responded: model={Model} apiModel={ApiModel} contentLen={Len} parsedBatches={BatchCount} parsedSteps={StepCount} parseError={Err}",
            request.ModelId, model.ApiModelId, raw.Length, parsed.Count, CountSteps(parsed), parseError ?? "<none>");

        return new PlanProposalResponse(parsed, raw, parseError);
    }

    private static string BuildSystemPrompt(PlanProposalRequest request)
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
        sb.AppendLine("- DayA must equal DayB (only same-day swaps are accepted in this version).");
        sb.AppendLine("- RowA must differ from RowB.");
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

    private static string BuildUserMessage(PlanProposalRequest request)
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
        AppendFocusedGoal(sb, request.FocusedIntent);
        sb.AppendLine();
        sb.AppendLine("Now reply with the JSON object as defined in the system prompt. No other text.");
        return sb.ToString();
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
        sb.AppendLine("PRIOR REJECTIONS (do not repeat these patterns):");
        for (var i = 0; i < rejections.Count; i++)
        {
            var entry = rejections[i];
            sb.AppendLine($"- [{entry.Intent}/{entry.Result}] {entry.Summary}");
        }
        sb.AppendLine();

        var forbiddenKeys = CollectForbiddenSwapKeys(rejections);
        if (forbiddenKeys.Count == 0)
        {
            return;
        }
        sb.AppendLine("DO NOT REPEAT THESE EXACT SWAPS (auto-rejected if proposed again, host validates strictly):");
        for (var i = 0; i < forbiddenKeys.Count; i++)
        {
            var key = forbiddenKeys[i];
            sb.AppendLine($"- row {key.RowSmaller} <-> row {key.RowLarger} on day {key.Day}");
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

    private const int MaxBatchesPerResponse = 3;

    private List<MutationBatch> TryParseBatches(string raw, int maxStepsPerBatch, int iterationIndex, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(raw))
        {
            error = "Model returned empty content. Reasoning models often consume the entire token budget on internal chain-of-thought; try a non-reasoning model (e.g. deepseek-v4-flash, llama-3-3-70b-versatile) or raise max_tokens.";
            return [];
        }

        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            error = "No JSON object found in LLM response.";
            return [];
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("batches", out var batchesEl) || batchesEl.ValueKind != JsonValueKind.Array)
            {
                error = "JSON missing 'batches' array.";
                return [];
            }

            var result = new List<MutationBatch>();
            foreach (var batchEl in batchesEl.EnumerateArray())
            {
                if (result.Count >= MaxBatchesPerResponse)
                {
                    break;
                }
                if (!TryReadBatch(batchEl, maxStepsPerBatch, iterationIndex, out var batch))
                {
                    continue;
                }
                result.Add(batch);
            }
            return result;
        }
        catch (JsonException ex)
        {
            error = $"JSON parse failed: {ex.Message}";
            _logger.LogWarning(ex, "Holistic Harmonizer LLM JSON parse failed; raw response length {Length}", raw.Length);
            return [];
        }
    }

    private static bool TryReadBatch(JsonElement el, int maxStepsPerBatch, int iterationIndex, out MutationBatch batch)
    {
        batch = default!;
        if (el.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var intent = el.TryGetProperty("intent", out var intentEl) && intentEl.ValueKind == JsonValueKind.String
            ? intentEl.GetString() ?? string.Empty
            : string.Empty;
        if (string.IsNullOrWhiteSpace(intent))
        {
            return false;
        }

        if (!el.TryGetProperty("steps", out var stepsEl) || stepsEl.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var steps = new List<PlanCellSwap>();
        foreach (var stepEl in stepsEl.EnumerateArray())
        {
            if (steps.Count >= maxStepsPerBatch)
            {
                break;
            }
            if (TryReadSwap(stepEl, out var swap))
            {
                steps.Add(swap);
            }
        }

        if (steps.Count == 0)
        {
            return false;
        }

        batch = new MutationBatch(Guid.NewGuid(), intent, iterationIndex, steps);
        return true;
    }

    private static int CountSteps(IReadOnlyList<MutationBatch> batches)
    {
        var total = 0;
        for (var i = 0; i < batches.Count; i++)
        {
            total += batches[i].Steps.Count;
        }
        return total;
    }

    private static bool TryReadSwap(JsonElement el, out PlanCellSwap swap)
    {
        swap = default!;
        if (el.ValueKind != JsonValueKind.Object)
        {
            return false;
        }
        if (!el.TryGetProperty("rowA", out var rowAEl) || !rowAEl.TryGetInt32(out var rowA))
        {
            return false;
        }
        if (!el.TryGetProperty("dayA", out var dayAEl) || !dayAEl.TryGetInt32(out var dayA))
        {
            return false;
        }
        if (!el.TryGetProperty("rowB", out var rowBEl) || !rowBEl.TryGetInt32(out var rowB))
        {
            return false;
        }
        if (!el.TryGetProperty("dayB", out var dayBEl) || !dayBEl.TryGetInt32(out var dayB))
        {
            return false;
        }
        var reason = el.TryGetProperty("reason", out var reasonEl) && reasonEl.ValueKind == JsonValueKind.String
            ? reasonEl.GetString() ?? string.Empty
            : string.Empty;
        swap = new PlanCellSwap(rowA, dayA, rowB, dayB, reason);
        return true;
    }

    private static string? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }
        var match = JsonObjectRegex().Match(raw);
        return match.Success ? match.Value : null;
    }

    [GeneratedRegex(@"\{[\s\S]*\}", RegexOptions.Compiled)]
    private static partial Regex JsonObjectRegex();
}
