// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// One set_order_review_decision call, parsed: the answers the user gave for the targeted orders. Every value is
/// optional; null leaves that aspect as it is.
/// </summary>
/// <param name="Target">Review keys, order ids or "all"</param>
/// <param name="StartTime">New start of a full-day order (the same time keeps the start)</param>
/// <param name="Split">"3x8", "2x12", "none" or explicit parts "HH:mm-HH:mm,..."</param>
/// <param name="PartNames">Names of the parts, comma separated, in part order</param>
/// <param name="GroupName">Group to staff an order that has none</param>
/// <param name="Qualifications">"none" or qualification names, comma separated</param>
/// <param name="Expenses">Expenses the user named for the targeted orders</param>
/// <param name="ExpenseDecline">DeclinedOnce (none for these orders) or DeclinedFinal (none for any order left)</param>
/// <param name="AcceptProposals">Take the proposed start, split, group and qualifications where still undecided</param>
/// <param name="Skip">Leave the targeted orders out of this review</param>
public sealed record OrderReviewDecision(
    string Target,
    TimeOnly? StartTime,
    string? Split,
    string? PartNames,
    string? GroupName,
    string? Qualifications,
    IReadOnlyList<OrderReviewExpense>? Expenses,
    OrderReviewExpenseStatus? ExpenseDecline,
    bool AcceptProposals,
    bool Skip);
