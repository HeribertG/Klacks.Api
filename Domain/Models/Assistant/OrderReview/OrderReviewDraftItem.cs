// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One open order in the order review. ReviewKey names its bundle (orders with the same pattern, "B1") or marks it as
/// an outlier reviewed on its own ("S1", with OutlierReason). The Proposed* values are the server's suggestions, the
/// Chosen* values the user's decisions (null: not decided yet; an empty ChosenParts list means "do not split").
/// BlockingFields are order fields the review cannot fill (complete them in the order first). A start change, a new
/// group, required qualifications and expenses are written to the order before it is sealed and cut.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant.OrderReview;

public sealed class OrderReviewDraftItem
{
    public Guid OrderId { get; set; }

    public string OrderName { get; set; } = string.Empty;

    public string CustomerName { get; set; } = string.Empty;

    public DateOnly FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public string ReviewKey { get; set; } = string.Empty;

    public string? OutlierReason { get; set; }

    public List<string> BlockingFields { get; set; } = [];

    public bool IsFullDay { get; set; }

    public TimeOnly CurrentStart { get; set; }

    public TimeOnly CurrentEnd { get; set; }

    public TimeOnly? ProposedStart { get; set; }

    public TimeOnly? ChosenStart { get; set; }

    public List<OrderReviewPart> ProposedParts { get; set; } = [];

    public List<OrderReviewPart>? ChosenParts { get; set; }

    public OrderSplitVerdictKind? SplitVerdict { get; set; }

    public int SplitAllowedMembers { get; set; }

    public int SplitMemberCount { get; set; }

    public bool HasGroup { get; set; }

    public string? CurrentGroupName { get; set; }

    public Guid? ProposedGroupId { get; set; }

    public string? ProposedGroupName { get; set; }

    public string? GroupSource { get; set; }

    public Guid? ChosenGroupId { get; set; }

    public string? ChosenGroupName { get; set; }

    public List<Guid> ProposedQualificationIds { get; set; } = [];

    public List<string> ProposedQualificationNames { get; set; } = [];

    public List<Guid>? ChosenQualificationIds { get; set; }

    public List<string> ChosenQualificationNames { get; set; } = [];

    public OrderReviewExpenseStatus ExpenseStatus { get; set; }

    public List<OrderReviewExpense> Expenses { get; set; } = [];

    public OrderReviewExecutionStatus ExecutionStatus { get; set; }

    public string? FailureReason { get; set; }

    public Guid? PlannableShiftId { get; set; }
}
