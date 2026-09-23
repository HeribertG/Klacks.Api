// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs the two clarification-dialog hooks for the channel adapters (EmailPollingBackgroundService,
/// MessengerIntentProcessor) so that the dialog can never break the regular inbound path. The
/// IClarificationCoordinator is resolved inside the guarded block, so a DI resolution failure degrades
/// exactly like a coordinator failure: BeforeAnalysisSafelyAsync then answers None (the message is
/// analyzed normally) and AfterAnalysisSafelyAsync answers Continue (the message stays on the regular
/// path). An OperationCanceledException is only rethrown when the caller's token is cancelled.
/// </summary>
/// <param name="serviceProvider">Service provider of the adapter's scope the coordinator is resolved from</param>
/// <param name="request">The inbound message as the clarification dialog sees it</param>
/// <param name="analysis">The in-memory regular analysis (never a re-loaded copy: some flags are not persisted)</param>
/// <param name="logger">The adapter's logger for the degradation warning</param>

using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Infrastructure.Inbound;

internal static class ClarificationDialogSafeGuard
{
    internal static async Task<ClarificationPreAnalysis> BeforeAnalysisSafelyAsync(
        IServiceProvider serviceProvider, ClarificationRequest request, ILogger logger, CancellationToken cancellationToken)
    {
        try
        {
            return await serviceProvider.GetRequiredService<IClarificationCoordinator>()
                .BeforeAnalysisAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Clarification answer check failed for message {MessageId}; the message is analyzed normally",
                request.Source.SourceId);
            return ClarificationPreAnalysis.None;
        }
    }

    internal static async Task<ClarificationPostAnalysis> AfterAnalysisSafelyAsync(
        IServiceProvider serviceProvider,
        ClarificationRequest request,
        InboundAnalysis analysis,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            return await serviceProvider.GetRequiredService<IClarificationCoordinator>()
                .AfterAnalysisAsync(request, analysis, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Clarification check failed for message {MessageId}; the message stays on the regular path",
                request.Source.SourceId);
            return ClarificationPostAnalysis.Continue;
        }
    }
}
