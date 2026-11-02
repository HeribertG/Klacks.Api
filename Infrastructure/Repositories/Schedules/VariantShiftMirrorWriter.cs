// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Keeps the two rows of a shift in a live variant (key) equal in the same save: after the sealed order or its plannable
/// copy (OriginalShift) was written, the mirrored fields (VariantShiftMirror), the group items, the expenses and the
/// required qualifications are carried onto the other row. The collections are rebuilt from a ShiftCloner copy and
/// ShiftRequirementMaterializer, so the group items of both rows carry the token of the key and no tracked instance is
/// shared between the rows. Nothing happens for a shift without a live variant token, for a cut piece, or when the
/// other row is missing. A static helper, because the repository is near its size limit and already holds the context.
/// </summary>
/// <param name="context">Database context of the calling repository</param>
/// <param name="collectionUpdateService">Syncs the collections of the other row</param>
/// <param name="written">The tracked row that was just written, with its collections loaded</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class VariantShiftMirrorWriter
{
    public static async Task MirrorAsync(
        DataBaseContext context, EntityCollectionUpdateService collectionUpdateService, Shift written)
    {
        if (written.AnalyseToken is not { } token
            || written.Status is not (ShiftStatus.SealedOrder or ShiftStatus.OriginalShift)
            || !await ScenarioVariantQueries.LiveVariants(context).AnyAsync(v => v.Token == token))
        {
            return;
        }

        var partner = await FindPartnerAsync(context, written, token);
        if (partner == null)
        {
            return;
        }

        VariantShiftMirror.Copy(written, partner);

        var fresh = ShiftCloner.Clone(written);
        collectionUpdateService.UpdateCollection(
            partner.GroupItems, fresh.GroupItems, partner.Id, (item, shiftId) => item.ShiftId = shiftId, item => item.GroupId);
        collectionUpdateService.UpdateCollection(
            partner.ShiftExpenses, fresh.ShiftExpenses, partner.Id, (expense, shiftId) => expense.ShiftId = shiftId);
        collectionUpdateService.UpdateCollection(
            partner.RequiredQualifications,
            ShiftRequirementMaterializer.CopyAll(written.RequiredQualifications, partner.Id),
            partner.Id,
            (qualification, shiftId) => qualification.ShiftId = shiftId,
            qualification => qualification.QualificationId);
    }

    private static async Task<Shift?> FindPartnerAsync(DataBaseContext context, Shift written, Guid token)
    {
        var writtenId = written.Id;
        var sealedOrderId = written.OriginalId;
        var family = context.Shift.Where(s => !s.IsDeleted && s.AnalyseToken == token);

        if (written.Status == ShiftStatus.OriginalShift)
        {
            if (sealedOrderId == null)
            {
                return null;
            }

            family = family.Where(s => s.Id == sealedOrderId && s.Status == ShiftStatus.SealedOrder);
        }
        else
        {
            family = family.Where(s => s.OriginalId == writtenId && s.Status == ShiftStatus.OriginalShift);
        }

        return await family
            .Include(s => s.GroupItems)
            .Include(s => s.ShiftExpenses)
            .Include(s => s.RequiredQualifications)
            .AsSplitQuery()
            .FirstOrDefaultAsync();
    }
}
