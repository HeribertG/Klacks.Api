// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the Wizard 3 vision capability check against one model: paints a fresh random token into a small PNG,
/// asks the model to read it back and judges the answer. One read is a sample, not a verdict, so the probe only
/// reports "not vision-capable" (AnsweredButFailedImageCheck, which callers cache for hours) after
/// <see cref="ReadAttempts"/> independent reads with different tokens all failed. One correct read passes:
/// a text-only model guesses a token with probability 1/720. Transient capacity errors are retried once;
/// timeouts, provider errors and answers swallowed by reasoning stay inconclusive and are never reported as
/// a vision failure. All reads share one deadline, so the check never takes longer than a single read did before.
/// </summary>
/// <param name="logger">Diagnostic logger for failed reads and retries</param>
/// <param name="tokenSource">Produces the token for each read; random in production, fixed in tests</param>
/// <param name="pngRenderer">Renders the token into the test image</param>
/// <param name="totalTimeout">Upper bound for the whole check, all reads and retries included</param>
/// <param name="transientRetryDelay">Backoff before retrying a transient provider error</param>

using System.Diagnostics;
using System.Globalization;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Bitmap;
using Klacks.ScheduleOptimizer.HolisticHarmonizer.Llm;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

public sealed class VisionCapabilityProbe
{
    internal const int ReadAttempts = 2;
    internal const string TokenAlphabet = "EFHKLNPTXZ";
    private const int TransientRetries = 1;
    private const int TokenLength = 3;
    private const string PngFailedErrorFormat = "Capability PNG generation failed: {0}";
    private const string RequestFailedErrorFormat = "Capability check failed: {0}";
    private const string TimedOutErrorFormat = "Capability check timed out after {0:F0}s.";
    private const string NotVisionCapableErrorFormat =
        "{0} Failed {1} of {1} reads with different tokens - the model does not reliably process the bitmaps Wizard 3 needs.";
    private static readonly TimeSpan DefaultTotalTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan DefaultTransientRetryDelay = TimeSpan.FromSeconds(2);

    private readonly ILogger _logger;
    private readonly Func<string> _tokenSource;
    private readonly Func<string, byte[]> _pngRenderer;
    private readonly TimeSpan _totalTimeout;
    private readonly TimeSpan _transientRetryDelay;

    public VisionCapabilityProbe(ILogger logger)
        : this(logger, GenerateToken, VisionCapabilityPngRenderer.Render, DefaultTotalTimeout, DefaultTransientRetryDelay)
    {
    }

    internal VisionCapabilityProbe(
        ILogger logger,
        Func<string> tokenSource,
        Func<string, byte[]> pngRenderer,
        TimeSpan totalTimeout,
        TimeSpan transientRetryDelay)
    {
        _logger = logger;
        _tokenSource = tokenSource;
        _pngRenderer = pngRenderer;
        _totalTimeout = totalTimeout;
        _transientRetryDelay = transientRetryDelay;
    }

    public async Task<PlanProposalPingResult> RunAsync(LLMModel model, ILLMProvider provider, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(provider);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_totalTimeout);

        string? lastFailure = null;
        long lastLatencyMs = 0;

        for (var read = 1; read <= ReadAttempts; read++)
        {
            var expectedToken = _tokenSource();
            byte[] png;
            try
            {
                png = _pngRenderer(expectedToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wizard 3 vision capability PNG generation failed for model {ModelId}", model.ModelId);
                return new PlanProposalPingResult(false, 0, string.Format(CultureInfo.InvariantCulture, PngFailedErrorFormat, ex.Message));
            }

            var request = HolisticHarmonizerProbeRequests.Capability(model, png);
            var (verdict, latencyMs) = await SendWithTransientRetryAsync(
                provider, request, expectedToken, model.ModelId, cancellationToken, deadline.Token);
            lastLatencyMs = latencyMs;

            switch (verdict.Outcome)
            {
                case VisionCapabilityOutcome.Passed:
                    if (lastFailure is not null)
                    {
                        _logger.LogInformation(
                            "Wizard 3 vision check for {ModelId} passed on read {Read} after an earlier failed read: {Error}",
                            model.ModelId, read, lastFailure);
                    }

                    return new PlanProposalPingResult(true, latencyMs, null);

                case VisionCapabilityOutcome.Inconclusive:
                    return new PlanProposalPingResult(false, latencyMs, verdict.Error);

                default:
                    lastFailure = verdict.Error ?? string.Empty;
                    _logger.LogInformation(
                        "Wizard 3 vision check for {ModelId} read {Read}/{Reads} failed: {Error}",
                        model.ModelId, read, ReadAttempts, verdict.Error);
                    break;
            }
        }

        var error = string.Format(CultureInfo.InvariantCulture, NotVisionCapableErrorFormat, lastFailure, ReadAttempts);
        return new PlanProposalPingResult(false, lastLatencyMs, error, AnsweredButFailedImageCheck: true);
    }

    private async Task<(VisionCapabilityVerdict Verdict, long LatencyMs)> SendWithTransientRetryAsync(
        ILLMProvider provider,
        LLMProviderRequest request,
        string expectedToken,
        string modelId,
        CancellationToken callerToken,
        CancellationToken deadlineToken)
    {
        var stopwatch = Stopwatch.StartNew();
        for (var attempt = 0; ; attempt++)
        {
            stopwatch.Restart();
            LLMProviderResponse response;
            try
            {
                response = await provider.ProcessAsync(request, deadlineToken);
            }
            catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return (TimedOut(), stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Wizard 3 vision check threw for model {ModelId}", modelId);
                return (Inconclusive(string.Format(CultureInfo.InvariantCulture, RequestFailedErrorFormat, ex.Message)), stopwatch.ElapsedMilliseconds);
            }

            var latencyMs = stopwatch.ElapsedMilliseconds;

            // Several providers catch the cancellation themselves and return Success=false instead of throwing.
            if (!response.Success && deadlineToken.IsCancellationRequested)
            {
                callerToken.ThrowIfCancellationRequested();
                return (TimedOut(), latencyMs);
            }

            var verdict = VisionCapabilityResponseEvaluator.Evaluate(response, expectedToken, TokenAlphabet);
            var retryable = verdict.Outcome == VisionCapabilityOutcome.Inconclusive
                && !response.Success
                && TransientProviderErrorDetector.IsTransient(response.Error)
                && attempt < TransientRetries;
            if (!retryable)
            {
                return (verdict, latencyMs);
            }

            _logger.LogInformation(
                "Wizard 3 vision check transient failure for {ModelId}: {Error}; retrying after {Delay}s",
                modelId, response.Error, _transientRetryDelay.TotalSeconds);
            try
            {
                await Task.Delay(_transientRetryDelay, deadlineToken);
            }
            catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
            {
                return (TimedOut(), latencyMs);
            }
        }
    }

    private VisionCapabilityVerdict TimedOut() =>
        Inconclusive(string.Format(CultureInfo.InvariantCulture, TimedOutErrorFormat, _totalTimeout.TotalSeconds));

    private static VisionCapabilityVerdict Inconclusive(string error) =>
        new(VisionCapabilityOutcome.Inconclusive, error);

    private static string GenerateToken()
    {
        var letters = TokenAlphabet.ToCharArray();
        Random.Shared.Shuffle(letters);
        return new string(letters, 0, TokenLength);
    }
}
