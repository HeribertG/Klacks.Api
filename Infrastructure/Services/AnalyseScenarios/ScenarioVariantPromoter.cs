// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Promotes the variants (keys) a scenario carries when it is accepted: every live row of those keys and its live group
/// items lose the key token, the keys go Accepted, and the links stay as evidence. Status, customer and the OriginalId,
/// ParentId and RootId links stay as they are; a soft-deleted row keeps its token and stays out of every reader. Promote
/// only stages, so the accept commits it together with the works landing on those rows. The roots of the promoted trees
/// are read before the promote, because it clears the token that identifies them; numbering them needs the committed
/// rows, so it runs after the accept commit and the caller saves it. The rows of a key are the only ones touched: a
/// scenario without keys costs three reads of its links here (roots, promote, commit) and changes nothing.
/// The accept of a scenario with keys commits here, not through IUnitOfWork: two sibling scenarios sharing a key can both
/// pass the accept gate, so a key moves from Active to Accepted by a conditional update, and only the accept that wins it
/// saves its staged changes. The update and the save run in one transaction under the execution strategy, so the loser
/// changes nothing and a transient failure replays both: saving without accepting the changes keeps them tracked until the
/// commit went through. After a transient failure the strategy first reads whether the scenario is Accepted, so a commit
/// whose acknowledgement was lost counts as done and is not replayed into a lost key. When the commit fails for good (a lost
/// key, a database error, a cancellation) the tracker is cleared before the exception leaves, because the scope goes on
/// saving on this context and would write the staged accept. The keys are updated one by one in id order, which lets two
/// accepts wait for each other instead of deadlocking and names every lost key.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="shiftTreeService">Numbers the promoted trees</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioVariantPromoter
{
    private readonly DataBaseContext _context;
    private readonly IShiftTreeService _shiftTreeService;

    public ScenarioVariantPromoter(DataBaseContext context, IShiftTreeService shiftTreeService)
    {
        _context = context;
        _shiftTreeService = shiftTreeService;
    }

    /// <summary>
    /// Root ids of the live rows of the keys the scenario carries. Must be read before <see cref="PromoteAsync"/>.
    /// </summary>
    /// <param name="scenarioToken">Token of the scenario being accepted</param>
    internal async Task<IReadOnlyList<Guid>> GetTreeRootIdsAsync(Guid scenarioToken, CancellationToken ct)
    {
        var variantTokens = await ReadVariantTokensAsync(scenarioToken, ct);
        if (variantTokens.Count == 0)
        {
            return [];
        }

        return await LiveRowsOf(variantTokens)
            .Where(shift => shift.RootId != null)
            .Select(shift => shift.RootId!.Value)
            .Distinct()
            .ToListAsync(ct);
    }

    /// <summary>
    /// Stages the promotion of the keys the scenario carries.
    /// </summary>
    /// <param name="scenarioToken">Token of the scenario being accepted</param>
    internal async Task PromoteAsync(Guid scenarioToken, CancellationToken ct)
    {
        var variantTokens = await ReadVariantTokensAsync(scenarioToken, ct);
        if (variantTokens.Count == 0)
        {
            return;
        }

        var shifts = await LiveRowsOf(variantTokens)
            .Include(shift => shift.GroupItems)
            .ToListAsync(ct);

        foreach (var shift in shifts)
        {
            shift.AnalyseToken = null;
            foreach (var groupItem in shift.GroupItems)
            {
                groupItem.AnalyseToken = null;
            }
        }
    }

    /// <summary>
    /// Commits a staged accept of a scenario with keys and moves the keys to Accepted in the same transaction. False for a
    /// scenario without keys: nothing happened and the caller commits as usual. Throws ScenarioVariantConsumedException when
    /// another accept took a key first; the transaction is rolled back and nothing is written. Every exception leaves with an
    /// empty tracker, so a later save in the scope writes nothing of the accept. A commit whose acknowledgement is lost is
    /// verified against the stored scenario and counts as done.
    /// </summary>
    /// <param name="scenarioToken">Token of the scenario being accepted</param>
    internal async Task<bool> TryCommitAcceptAsync(Guid scenarioToken, CancellationToken ct)
    {
        try
        {
            var variants = await ScenarioVariantQueries.LiveVariantsOfScenario(_context, scenarioToken)
                .AsNoTracking()
                .OrderBy(variant => variant.Id)
                .Select(variant => new CarriedVariant(variant.Id, variant.Name))
                .ToListAsync(ct);
            if (variants.Count == 0)
            {
                return false;
            }

            await CommitWithKeysAsync(scenarioToken, variants, ct);
        }
        catch
        {
            _context.ChangeTracker.Clear();
            throw;
        }

        _context.ChangeTracker.AcceptAllChanges();
        return true;
    }

    /// <summary>
    /// Stages the nested-set values of the given trees. Safe to repeat; the caller saves.
    /// </summary>
    /// <param name="rootIds">Roots of the promoted trees, read before the promote</param>
    internal Task RecalculateTreesAsync(IReadOnlyCollection<Guid> rootIds, CancellationToken ct)
        => _shiftTreeService.RecalculateTreesAsync(rootIds, ct);

    private async Task CommitWithKeysAsync(Guid scenarioToken, IReadOnlyList<CarriedVariant> variants, CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync<IReadOnlyList<CarriedVariant>, bool>(
                variants,
                (carried, token) => ConsumeKeysAndSaveAsync(carried, token),
                (_, token) => VerifyAcceptCommittedAsync(scenarioToken, token),
                ct);
        }
        catch (DbUpdateException ex)
        {
            throw DbUpdateExceptionTranslator.Translate(ex);
        }
    }

    private async Task<bool> ConsumeKeysAndSaveAsync(IReadOnlyList<CarriedVariant> variants, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);

        var consumed = new List<string>();
        foreach (var variant in variants)
        {
            if (!await TryConsumeAsync(variant.Id, ct))
            {
                consumed.Add(variant.Name);
            }
        }

        if (consumed.Count > 0)
        {
            throw new ScenarioVariantConsumedException(consumed.Order(StringComparer.Ordinal).ToList());
        }

        await _context.SaveChangesAsync(acceptAllChangesOnSuccess: false, ct);
        await transaction.CommitAsync(ct);
        return true;
    }

    private async Task<ExecutionResult<bool>> VerifyAcceptCommittedAsync(Guid scenarioToken, CancellationToken ct)
    {
        var committed = await _context.Set<AnalyseScenario>()
            .AsNoTracking()
            .AnyAsync(
                scenario => scenario.Token == scenarioToken
                    && !scenario.IsDeleted
                    && scenario.Kind == AnalyseScenarioKind.Scenario
                    && scenario.Status == AnalyseScenarioStatus.Accepted,
                ct);

        return new ExecutionResult<bool>(committed, committed);
    }

    private async Task<bool> TryConsumeAsync(Guid variantId, CancellationToken ct)
    {
        var stamp = BulkUpdateAuditStamp.Now(_context);
        var moved = await ScenarioVariantQueries.LiveVariants(_context)
            .Where(variant => variant.Id == variantId && variant.Status == AnalyseScenarioStatus.Active)
            .ExecuteUpdateAsync(
                setters => stamp.ApplyTo(setters.SetProperty(variant => variant.Status, AnalyseScenarioStatus.Accepted)),
                ct);

        return moved == 1;
    }

    private Task<List<Guid>> ReadVariantTokensAsync(Guid scenarioToken, CancellationToken ct)
        => ScenarioVariantQueries.LiveVariantsOfScenario(_context, scenarioToken)
            .Select(variant => variant.Token)
            .ToListAsync(ct);

    private IQueryable<Shift> LiveRowsOf(IReadOnlyCollection<Guid> variantTokens)
        => _context.Shift.Where(shift => shift.AnalyseToken != null && variantTokens.Contains(shift.AnalyseToken.Value));

    private sealed record CarriedVariant(Guid Id, string Name);
}
