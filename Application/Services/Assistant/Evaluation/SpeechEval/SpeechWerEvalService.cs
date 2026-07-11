// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates a speech-wer eval run: loads the goldset, skips items whose audio file is
/// not present, transcribes the remaining recordings through the STT seam, scores word
/// error rate, name accuracy and composite per item, and persists one EvalRun with
/// regression against the latest run of the same goldset and STT provider.
/// </summary>
/// <param name="sttModelOrProviderId">STT provider id to evaluate (e.g. "groq-whisper")</param>

using System.Diagnostics;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public class SpeechWerEvalService : ISpeechWerEvalService
{
    private readonly ISpeechGoldsetLoader _goldsetLoader;
    private readonly ISpeechTranscriptionService _transcriptionService;
    private readonly IEvalRunRepository _evalRunRepository;
    private readonly ILogger<SpeechWerEvalService> _logger;

    public SpeechWerEvalService(
        ISpeechGoldsetLoader goldsetLoader,
        ISpeechTranscriptionService transcriptionService,
        IEvalRunRepository evalRunRepository,
        ILogger<SpeechWerEvalService> logger)
    {
        _goldsetLoader = goldsetLoader;
        _transcriptionService = transcriptionService;
        _evalRunRepository = evalRunRepository;
        _logger = logger;
    }

    public async Task<SpeechWerEvalRunResult> RunAsync(string sttModelOrProviderId, CancellationToken cancellationToken = default)
    {
        var items = await _goldsetLoader.LoadAsync(SpeechEvalConstants.GoldsetName, cancellationToken);

        var runStopwatch = Stopwatch.StartNew();
        var itemResults = new List<SpeechWerEvalItemResult>(items.Count);

        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            itemResults.Add(await EvaluateItemAsync(item, sttModelOrProviderId, cancellationToken));
        }

        runStopwatch.Stop();

        var measured = itemResults.Where(r => !r.Skipped).ToList();
        var dimensions = Aggregate(itemResults, measured);

        if (measured.Count == 0)
        {
            _logger.LogWarning(
                "SpeechWerEvalRun {Goldset} provider {Provider}: no audio files present, nothing measured",
                SpeechEvalConstants.GoldsetName.ForLog(), sttModelOrProviderId.ForLog());

            return new SpeechWerEvalRunResult
            {
                Run = null,
                Dimensions = dimensions,
                Items = itemResults,
                Message = SpeechEvalConstants.NoMeasurableItemsMessage
            };
        }

        var composite = measured.Average(r => r.Composite!.Value);

        var baseline = await _evalRunRepository.GetLatestAsync(SpeechEvalConstants.GoldsetName, sttModelOrProviderId, cancellationToken);
        decimal? regression = baseline == null ? null : (decimal)composite - baseline.CompositeScore;

        var evalRun = new EvalRun
        {
            Id = Guid.NewGuid(),
            Goldset = SpeechEvalConstants.GoldsetName,
            Provider = sttModelOrProviderId,
            Model = sttModelOrProviderId,
            CompositeScore = (decimal)composite,
            DimensionsJson = JsonSerializer.Serialize(dimensions),
            RegressionVsBaseline = regression,
            ItemsTotal = dimensions.ItemsTotal,
            ItemsPassed = dimensions.ItemsMeasured,
            DurationMs = (int)runStopwatch.ElapsedMilliseconds,
            CreateTime = DateTime.UtcNow
        };

        await _evalRunRepository.AddAsync(evalRun, cancellationToken);

        _logger.LogInformation(
            "SpeechWerEvalRun {Goldset} provider {Provider}: composite={Composite:F4}, avgWer={AvgWer:F4}, nameAccuracy={NameAccuracy:F4}, items={Items}, measured={Measured}, skipped={Skipped}, regression={Regression}",
            SpeechEvalConstants.GoldsetName.ForLog(), sttModelOrProviderId.ForLog(), composite,
            dimensions.AvgWer ?? -1, dimensions.NameAccuracy ?? -1,
            dimensions.ItemsTotal, dimensions.ItemsMeasured, dimensions.ItemsSkipped, regression);

        return new SpeechWerEvalRunResult
        {
            Run = evalRun,
            Dimensions = dimensions,
            Items = itemResults
        };
    }

    private async Task<SpeechWerEvalItemResult> EvaluateItemAsync(
        SpeechGoldsetItem item,
        string sttModelOrProviderId,
        CancellationToken cancellationToken)
    {
        var audioPath = _goldsetLoader.ResolveAudioPath(item.AudioFile);
        if (!File.Exists(audioPath))
        {
            return new SpeechWerEvalItemResult
            {
                ItemId = item.Id,
                AudioFile = item.AudioFile,
                Skipped = true
            };
        }

        var audio = await File.ReadAllBytesAsync(audioPath, cancellationToken);

        var itemStopwatch = Stopwatch.StartNew();
        var transcript = await _transcriptionService.TranscribeAsync(sttModelOrProviderId, audio, item.Locale, cancellationToken);
        itemStopwatch.Stop();

        var wer = WordErrorRate.Compute(item.ReferenceText, transcript);
        var nameAccuracy = WordErrorRate.ComputeNameAccuracy(transcript, item.ExpectedNames);

        return new SpeechWerEvalItemResult
        {
            ItemId = item.Id,
            AudioFile = item.AudioFile,
            Skipped = false,
            Wer = wer,
            NameAccuracy = nameAccuracy,
            Composite = WordErrorRate.ComputeComposite(wer, nameAccuracy),
            LatencyMs = itemStopwatch.Elapsed.TotalMilliseconds,
            Transcript = transcript
        };
    }

    private static SpeechWerEvalDimensions Aggregate(
        IReadOnlyList<SpeechWerEvalItemResult> allItems,
        IReadOnlyList<SpeechWerEvalItemResult> measured)
    {
        return new SpeechWerEvalDimensions(
            AvgWer: measured.Count == 0 ? null : measured.Average(r => r.Wer!.Value),
            NameAccuracy: measured.Count == 0 ? null : measured.Average(r => r.NameAccuracy!.Value),
            ItemsTotal: allItems.Count,
            ItemsMeasured: measured.Count,
            ItemsSkipped: allItems.Count - measured.Count,
            AvgLatencyMs: measured.Count == 0 ? 0 : measured.Average(r => r.LatencyMs));
    }
}
