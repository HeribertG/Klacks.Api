// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a split answer into the parts for one order: "3x8" and "2x12" split a full-day order equally from its start,
/// "none" leaves it whole, and explicit parts "HH:mm-HH:mm,..." must tile the order exactly (ShiftTilingValidator) -
/// a full-day order from its (chosen) start round the clock, any other order its own times. Part names are matched to
/// the parts by position. Pure.
/// </summary>

using Klacks.Api.Application.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewSplitSpec
{
    public const string ThreeByEight = "3x8";
    public const string TwoByTwelve = "2x12";
    public const string None = "none";

    private const char PartSeparator = ',';
    private const char RangeSeparator = '-';
    private const int RangeBoundCount = 2;

    private const string EqualSplitNeedsFullDayFormat =
        "'{0}' splits a full-day order; '{1}' runs {2}-{3}. Give explicit parts or 'none'.";
    private const string PartCountFormat = "A split needs between {0} and {1} parts; received {2}. Use 'none' to keep the order whole.";
    private const string InvalidPartFormat = "Part '{0}' cannot be read. Use 'HH:mm-HH:mm', several separated by commas.";

    public static (List<OrderReviewPart>? Parts, string? Error) Resolve(
        OrderReviewDraftItem item, string spec, string? partNames, string? language)
    {
        var start = item.ChosenStart ?? item.ProposedStart ?? item.CurrentStart;
        (List<OrderReviewPart>? Parts, string? Error) resolved = spec.ToLowerInvariant() switch
        {
            None => (new List<OrderReviewPart>(), null),
            ThreeByEight => EqualSplit(item, start, OrderReviewDefaults.StandardPartCount, spec),
            TwoByTwelve => EqualSplit(item, start, OrderReviewDefaults.AlternativePartCount, spec),
            _ => ExplicitSplit(item, start, spec, language),
        };

        if (resolved.Parts == null)
        {
            return (null, resolved.Error);
        }

        ApplyNames(resolved.Parts, partNames);
        return (resolved.Parts, null);
    }

    public static bool IsEqualSplitFrom(IReadOnlyList<OrderReviewPart> parts, TimeOnly start) =>
        parts.Count > 0
        && parts[0].Start == start
        && parts.Select(FullDayOrderRule.PartHours).Distinct().Count() == 1;

    private static (List<OrderReviewPart>? Parts, string? Error) EqualSplit(
        OrderReviewDraftItem item, TimeOnly start, int partCount, string spec)
    {
        if (!item.IsFullDay)
        {
            return (null, string.Format(
                EqualSplitNeedsFullDayFormat, spec, item.OrderName,
                OrderReviewPresenter.Time(item.CurrentStart), OrderReviewPresenter.Time(item.CurrentEnd)));
        }

        return (FullDayOrderRule.EqualParts(start, partCount), null);
    }

    private static (List<OrderReviewPart>? Parts, string? Error) ExplicitSplit(
        OrderReviewDraftItem item, TimeOnly start, string spec, string? language)
    {
        var ranges = new List<(TimeOnly Start, TimeOnly End)>();
        foreach (var entry in spec.Split(PartSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var bounds = entry.Split(RangeSeparator, StringSplitOptions.TrimEntries);
            if (bounds.Length != RangeBoundCount
                || !SkillCalendarStringParser.TryParseTimeOnly(bounds[0], language, out var partStart)
                || !SkillCalendarStringParser.TryParseTimeOnly(bounds[1], language, out var partEnd))
            {
                return (null, string.Format(InvalidPartFormat, entry));
            }

            ranges.Add((partStart, partEnd));
        }

        if (ranges.Count < OrderReviewDefaults.MinCutParts || ranges.Count > OrderReviewDefaults.MaxCutParts)
        {
            return (null, string.Format(PartCountFormat, OrderReviewDefaults.MinCutParts, OrderReviewDefaults.MaxCutParts, ranges.Count));
        }

        var orderStart = item.IsFullDay ? start : item.CurrentStart;
        var orderEnd = item.IsFullDay ? start : item.CurrentEnd;
        var tilingError = ShiftTilingValidator.Validate(orderStart, orderEnd, ranges);
        if (tilingError != null)
        {
            return (null, tilingError);
        }

        return (ranges.Select(range => new OrderReviewPart { Start = range.Start, End = range.End }).ToList(), null);
    }

    private static void ApplyNames(List<OrderReviewPart> parts, string? partNames)
    {
        if (string.IsNullOrWhiteSpace(partNames))
        {
            return;
        }

        var names = partNames.Split(PartSeparator, StringSplitOptions.TrimEntries);
        for (var index = 0; index < parts.Count && index < names.Length; index++)
        {
            parts[index].Name = names[index];
        }
    }
}
