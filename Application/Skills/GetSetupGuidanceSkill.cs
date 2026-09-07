// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answers "how do I get started" for an installation that cannot be scheduled yet, by reporting the
/// facts both possible routes depend on rather than by writing the advice itself. The wording is left
/// to the conversation; what this returns is checkable: which of the three setup stages the
/// installation is in, and for the ERP route the resolved drop-point folder, its sub-folders, the poll
/// schedule and how many import tokens exist — read from the same sources the import runner uses, so
/// the answer cannot drift from reality. For the manual route it returns the pages an order and a
/// shift are created on, taken from navigation-targets.json rather than spelled out here.
///
/// The manual-route wording is pinned to what the code actually does, verified 2026-09-07: no path
/// creates ShiftStatus.OriginalShift except sealing (ShiftRepository lines 488 and 553 plus
/// ShiftResetService), so "create a shift directly instead of an order" — the intuitive framing, and
/// the one the feature was requested with — does not exist in this system and must not be offered.
/// The real choice is when to seal.
///
/// A customer is NOT part of that requirement, and saying otherwise would hide a feature that exists:
/// OrderSealingService.CollectMissingRequirements never asks for one, Shift.ClientId is nullable, and
/// the Plannable Shifts view carries an admin-only New button (all-shift-list.component.html) that
/// creates the order clientless and hides the customer card. Only the create_shift SKILL demands a
/// clientId — that is a limitation of one entry point, not of the domain.
///
/// Split this way on purpose: an LLM inventing a folder path or a menu entry is the failure mode that
/// makes setup advice worse than none at all, while an LLM phrasing a question about an ERP is exactly
/// what it is good at. Everything factual therefore travels in Data; Message states the same facts in
/// one line so a model that ignores structured data still cannot make them up.
///
/// Reachable independently of the one-off no_schedule_yet notification (which dedups permanently),
/// which is what makes the guide re-enterable after the user abandons it.
///
/// The same skill also drives the guided setup consultation recipe through the optional
/// <c>phase</c> parameter, so one implementation stays the single source of truth for the route
/// logic instead of forking it across skills:
/// - Absent (the default, free-form model call): behaves exactly as before, no route is resolved
///   into navigation.
/// - <c>intro</c>: the opening turn, before either question has been answered. The route is
///   deliberately withheld from the data (see below) so the model cannot state a route before the
///   user has answered anything.
/// - <c>route</c>: after both questions were answered, reports the resolved route as data for the
///   conversation to explain in words.
/// - <c>act</c>: after the follow-up choice was made. Only navigates (<see cref="SkillResultType.Navigation"/>)
///   when the classified choice is "show me where"; every other choice, including an unclear
///   attribution that could otherwise assemble into a create offer, stays data-only.
/// </summary>
/// <param name="mediator">Resolves the default ERP drop point and its import tokens.</param>
/// <param name="activityProbe">Installation-wide setup snapshot along the order -> shift -> assignment chain.</param>
/// <param name="objectStorageService">Resolves the drop point's bucket prefix to an absolute on-disk path.</param>
/// <param name="settingsReader">Reads the import poll schedule and its time zone.</param>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpImportTokens;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_setup_guidance")]
public class GetSetupGuidanceSkill : BaseSkillImplementation
{
    private const string OrderListTarget = "shift-list";
    private const string NewShiftTarget = "new-shift";
    private const string CutShiftTarget = "cut-shift";
    private const string ScheduleTarget = "schedule";

    private readonly IMediator _mediator;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ISettingsReader _settingsReader;

    public GetSetupGuidanceSkill(
        IMediator mediator,
        IScheduleActivityProbe activityProbe,
        IObjectStorageService objectStorageService,
        ISettingsReader settingsReader)
    {
        _mediator = mediator;
        _activityProbe = activityProbe;
        _objectStorageService = objectStorageService;
        _settingsReader = settingsReader;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var state = await _activityProbe.GetSetupStateAsync(cancellationToken);
        if (state.HasWork)
        {
            return SkillResult.SuccessResult(
                new { SetupComplete = true, Installation = new { state.HasOrders, state.HasShifts, state.HasWork } },
                "Setup is complete — orders, shifts and work assignments all exist, so there is nothing "
                + "left to set up before scheduling.");
        }

        var phase = GetParameter<string>(parameters, SetupConsultationParameters.Phase);
        var attribution = SetupConsultationAnswerClassifier.ClassifyAttribution(
            GetParameter<string>(parameters, SetupConsultationParameters.Attribution));
        var orderSource = SetupConsultationAnswerClassifier.ClassifyOrderSource(
            GetParameter<string>(parameters, SetupConsultationParameters.OrderSource));
        var nextStep = SetupConsultationAnswerClassifier.ClassifyNextStep(
            GetParameter<string>(parameters, SetupConsultationParameters.NextStep));

        var stage = ScheduleSetupStages.For(state);
        var erpRoute = await BuildErpRouteAsync(cancellationToken);
        var route = SetupRouteResolver.Resolve(
            state, attribution, orderSource, context.UserPermissions.Contains(Roles.Admin));

        var isIntro = string.Equals(phase, SetupConsultationPhases.Intro, StringComparison.OrdinalIgnoreCase);
        var data = BuildData(state, stage, erpRoute, isIntro ? null : route, attribution, orderSource);

        if (string.Equals(phase, SetupConsultationPhases.Act, StringComparison.OrdinalIgnoreCase)
            && nextStep == SetupNextStepChoice.Show)
        {
            return SkillResult.Navigation(
                new { Route = route.ShowTarget, Target = route.ShowTarget },
                BuildMessage(stage, erpRoute));
        }

        return SkillResult.SuccessResult(data, BuildMessage(stage, erpRoute));
    }

    private object BuildData(
        ScheduleSetupState state,
        ScheduleSetupStage stage,
        ErpRouteFacts erpRoute,
        SetupRouteFacts? route,
        SetupAttributionAnswer attribution,
        SetupOrderSourceAnswer orderSource) => new
    {
        SetupComplete = false,
        Stage = stage.ToString(),
        Installation = new
        {
            state.HasOrders,
            state.HasShifts,
            state.HasWork,
            state.HasCustomers,
            state.HasGroups
        },
        Answers = new
        {
            Attribution = attribution.ToString(),
            OrderSource = orderSource.ToString()
        },
        Route = route == null
            ? null
            : new
            {
                Kind = route.Kind.ToString(),
                route.ShowTarget,
                route.MissingPrerequisites,
                route.RequiresAdmin
            },
        Handoff = route?.HandoffPhrase == null
            ? null
            : new { Label = route.HandoffPhrase, Value = route.HandoffPhrase },
        ErpRoute = erpRoute,
        ManualRoute = BuildManualRoute()
    };

    private object BuildManualRoute() => new
    {
        OrderMeaning =
            "An order records work to be done. Naming a customer makes the working hours "
            + "attributable to that customer: customer -> order -> shift -> hours. A customer is "
            + "NOT required, though.",
        ShiftMeaning =
            "A plannable shift is not created directly. It only ever comes into existence by "
            + "sealing an order: sealing marks the order immutable and creates the plannable "
            + "shift derived from it, in the same transaction. Sealing cannot be undone.",
        TheOnlyChoice =
            "The choice is therefore not 'order or shift' but WHEN to seal: creating an order "
            + "as a draft leaves it editable and produces no shift yet, while creating it "
            + "without the draft flag seals it at once and produces the plannable shift "
            + "immediately.",
        ClientlessDuties =
            "A duty whose hours are attributable to no single customer is created through the "
            + "Plannable Shifts view's own New button (administrators only), which hides the "
            + "customer card entirely. Two kinds: work caused by the orders but chargeable to "
            + "none of them (refuelling, vehicle care, cleaning, back office), and businesses "
            + "where no customer places an order at all - a ward, a kitchen, a salon. In those, "
            + "EVERY duty is clientless, so an empty customer must never be read as an "
            + "incomplete record.",
        ClientlessIsSealedOnCreation =
            "Such a duty is created SEALED, never as a draft: a draft is a customer's request "
            + "still being worked out, so a draft without a customer could not be told apart "
            + "from one where the customer is merely still missing. A draft without a customer "
            + "is therefore refused, and a clientless duty must be complete when it is created, "
            + "because sealing cannot be undone.",
        HoursCountEitherWay =
            "Attribution decides WHOSE the hours are, not whether they count. A clientless duty "
            + "is paid working time and enters target/actual hours, wages, supplements and rest "
            + "periods exactly like any other.",
        ClientlessTarget = OrderListTarget,
        WhenToUseADraft =
            "Keep the order a draft when details are still missing or somebody has to check "
            + "it, and when orders arrive from an outside system — an import always delivers "
            + "drafts and never seals anything by itself.",
        WhenToSealImmediately =
            "Seal at once when the order is complete and the duty should become plannable "
            + "right away.",
        FullDayOrders =
            "A duty spanning a whole day is ONE order over the full span, cut into its parts "
            + "afterwards — never several orders.",
        OrderAndShiftListTarget = OrderListTarget,
        NewShiftTarget,
        CutFullDayShiftTarget = CutShiftTarget,
        ScheduleTarget
    };

    private async Task<ErpRouteFacts> BuildErpRouteAsync(CancellationToken cancellationToken)
    {
        var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);
        if (dropPoint == null)
        {
            return new ErpRouteFacts(false, null, null, null, null, null, null, null, null, 0);
        }

        var normalizedPrefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var absolutePath = _objectStorageService.ResolvePath(normalizedPrefix);

        var cronExpression = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronExpression))?.Value
            ?? ErpImportSettingsTypes.DefaultCronExpression;
        var timeZoneId = (await _settingsReader.GetSetting(ErpImportSettingsTypes.CronTimeZoneId))?.Value
            ?? ErpImportSettingsTypes.DefaultTimeZoneId;

        var tokens = await _mediator.Send(new GetErpImportTokensQuery(dropPoint.Id), cancellationToken);

        return new ErpRouteFacts(
            true,
            dropPoint.Name,
            dropPoint.IsEnabled,
            absolutePath,
            ErpImportStorageKeys.ProcessingSegment,
            ErpImportStorageKeys.ProcessedSegment,
            ErpImportStorageKeys.ErrorSegment,
            cronExpression,
            timeZoneId,
            tokens.Count);
    }

    private static string BuildMessage(ScheduleSetupStage stage, ErpRouteFacts erp)
    {
        var erpSentence = erp.DropPointConfigured
            ? $"An ERP handover point '{erp.Name}' exists ({(erp.IsEnabled == true ? "switched on" : "switched off")}), "
                + $"order XML files are copied to '{erp.AbsolutePath}' and picked up on schedule "
                + $"[{erp.CronExpression}] {erp.TimeZone}; {erp.TokenCount} import token(s) exist."
            : "No ERP handover point is configured yet, so the ERP route would have to be set up first.";

        return $"Setup stage: {stage}. {erpSentence} A plannable shift is never created directly: it "
            + "only comes into existence by sealing an order, which is irreversible. The choice is when "
            + "to seal — a draft order stays editable and produces no shift yet, an order created "
            + "without the draft flag is sealed at once and its shift exists immediately. A customer is "
            + "optional: duties nobody is billed for are created through the Plannable Shifts view's own "
            + "New button and seal without one. An import always delivers drafts and seals nothing by "
            + "itself.";
    }

    private sealed record ErpRouteFacts(
        bool DropPointConfigured,
        string? Name,
        bool? IsEnabled,
        string? AbsolutePath,
        string? ProcessingFolder,
        string? ProcessedFolder,
        string? ErrorFolder,
        string? CronExpression,
        string? TimeZone,
        int TokenCount);
}
