// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates an extended copy of an existing calculation macro. The copy's script is the original script
/// followed by the appended block, so the original logic is carried over verbatim and the original macro is
/// never modified. Before anything is stored: the appended block may only OUTPUT on channels the backend
/// processes, the combined script must pass the script validator, and the regression check must show on the test
/// grid that every surcharge channel the original emits with a non-zero value stays identical and that the result
/// channel 1 is either unchanged or the original result plus exactly the surcharges the block adds on channels the
/// original leaves at zero. The copy is stored as a plain custom macro (no standard function, no category) with
/// origin AssistantExtension, so it can never take a standard function away from the original; the assistant may
/// later rename or delete it, but not change its script (a new extended copy of the original is the way to change
/// it). The regression check runs under the cancellation token of the skill call, so a stopped turn stops it.
/// The appended block runs after the original script and sees everything the original declared: its IMPORT symbols,
/// its variables (with the values the original left in them) and its FUNCTIONs, so a block that sets channel 1 can
/// build the new total on the original total. OUTPUT statements may stand anywhere and in any order. A refusal by the
/// validator, or a regression check aborted because the copy fails at runtime where the original runs, repeats this
/// guidance; an abort caused by the original or by the check itself (original does not compile, no comparable input,
/// time budget) does not. The OUTPUT channel scan runs on the trimmed block that is stored.
/// </summary>
/// <param name="macroId">Optional. Id of the macro to extend; preferred over macroName.</param>
/// <param name="macroName">Optional. Name of the macro to extend, resolved via <see cref="MacroResolver"/>.</param>
/// <param name="name">Required. Name of the copy; must not match an existing macro name or the name of a template
/// shipped with Klacks (<see cref="MacroNameCollision"/>).</param>
/// <param name="additionalScript">Required. Script block appended to the original script. It may read the IMPORT
/// symbols, variables and FUNCTIONs of the original (without importing or declaring them again) and declare new IMPORT
/// symbols, variables with DIM and FUNCTIONs under names the original does not use. It may add surcharges on channels
/// 10-14 the original leaves at zero; channel 1 stays unchanged or is set to the original result plus exactly the added
/// surcharges.</param>
/// <param name="description">Optional. Description of the copy, applied to all core languages.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.Settings.Macros;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries.Settings.Macros;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("extend_macro")]
public class ExtendMacroSkill : BaseSkillImplementation
{
    private const string MacroIdParameter = "macroId";
    private const string MacroNameParameter = "macroName";
    private const string NameParameter = "name";
    private const string AdditionalScriptParameter = "additionalScript";
    private const string DescriptionParameter = "description";
    private const string ScriptSeparator = "\n";
    private const string DeviationSeparator = "; ";

    private const string NameRequiredMessage = "name is required: the extended copy needs its own macro name.";
    private const string BlockRequiredMessage =
        "additionalScript is required: provide the script block that is appended to the original script.";
    private const string SourceMissingMessage = "Either macroId or macroName must be provided.";
    private const string InvalidIdMessage = "'{0}' is not a valid macro id.";
    private const string IdNotFoundMessage = "No macro found with id '{0}'.";
    private const string AppendedBlockHint =
        "The appended block runs after the original script and can read everything the original declared: its IMPORT "
        + "symbols (do not import them again), its variables with the values the original left in them and its "
        + "FUNCTIONs. It must not IMPORT, DIM or declare a FUNCTION under a name the original already uses; new names "
        + "are fine. OUTPUT statements may stand anywhere in the block, also inside IF, loops or FUNCTIONs, and in any "
        + "order.";
    private const string ResultChannelRule =
        "Allowed changes: a surcharge on a channel 10-14 that the original leaves at 0 for that input. Channel 1 (the "
        + "result, 0 included) must either stay exactly as the original produces it, or be set by the block to the "
        + "original result plus exactly the surcharges the block adds, for example the variable or expression the "
        + "original outputs on channel 1 plus the new surcharge.";
    private const string ScriptInvalidMessage =
        "The extended script is not valid: {0} " + AppendedBlockHint
        + " List the macro with its script to see the names it declares.";
    private const string RegressionAbortedMessage = "The extended copy was not saved: {0}";
    private const string CopyRuntimeFailedMessage = RegressionAbortedMessage + " " + AppendedBlockHint;
    private const string RegressionFailedMessage =
        "The extended copy was not saved because it changes what the original macro '{0}' outputs. "
        + ResultChannelRule + " {1} deviation(s) in total, first {2}: {3}";
    private const string DeviationFormat = "[{0}] channel {1}: original {2}, copy {3}";
    private const string AcceptedTotalFormat = " (also accepted on channel 1: {0}, the original result plus the added surcharges)";
    private const string MissingValueText = "no value";
    private const string NotCreatedMessage = "The extended copy '{0}' could not be created.";
    private const string CreatedMessage =
        "Macro '{0}' was created as an extended copy of '{1}' (id {2}). The original macro is unchanged. The "
        + "regression check compared {3} test inputs ({4} skipped because the original itself does not run on "
        + "them). No shift or absence type uses the copy yet; it only takes effect once it is assigned.";

    private readonly IMediator _mediator;
    private readonly IMacroOutputChannelInspector _channelInspector;
    private readonly IMacroScriptValidator _scriptValidator;
    private readonly IMacroRegressionChecker _regressionChecker;

    public ExtendMacroSkill(
        IMediator mediator,
        IMacroOutputChannelInspector channelInspector,
        IMacroScriptValidator scriptValidator,
        IMacroRegressionChecker regressionChecker)
    {
        _mediator = mediator;
        _channelInspector = channelInspector;
        _scriptValidator = scriptValidator;
        _regressionChecker = regressionChecker;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var copyName = GetParameter<string>(parameters, NameParameter)?.Trim();
        if (string.IsNullOrWhiteSpace(copyName))
        {
            return SkillResult.Error(NameRequiredMessage);
        }

        var block = GetParameter<string>(parameters, AdditionalScriptParameter);
        if (string.IsNullOrWhiteSpace(block))
        {
            return SkillResult.Error(BlockRequiredMessage);
        }

        var macros = (await _mediator.Send(new ListQuery(), cancellationToken)).ToList();
        var (source, resolveError) = ResolveSource(parameters, macros);
        if (source == null)
        {
            return SkillResult.Error(resolveError!);
        }

        var nameRefusal = MacroNameCollision.FindRefusal(macros, copyName, null);
        if (nameRefusal != null)
        {
            return SkillResult.Error(nameRefusal);
        }

        var appendedBlock = block.Trim();
        var channelViolation = MacroOutputChannelPolicy.FindViolation(_channelInspector.Inspect(appendedBlock));
        if (channelViolation != null)
        {
            return SkillResult.Error(channelViolation);
        }

        var combined = source.Content.TrimEnd() + ScriptSeparator + appendedBlock;
        var validation = _scriptValidator.Validate(combined);
        if (!validation.IsValid)
        {
            return SkillResult.Error(Format(ScriptInvalidMessage, validation.ErrorMessage));
        }

        var regression = _regressionChecker.Check(source.Content, combined, cancellationToken);
        if (!regression.Passed)
        {
            return SkillResult.Error(DescribeRegressionFailure(source, regression));
        }

        return await StoreCopyAsync(
            source, copyName, combined, GetParameter<string>(parameters, DescriptionParameter), regression, cancellationToken);
    }

    private static (MacroResource? Source, string? Error) ResolveSource(
        Dictionary<string, object> parameters, IReadOnlyList<MacroResource> macros)
    {
        var rawId = GetParameter<string>(parameters, MacroIdParameter);
        if (!string.IsNullOrWhiteSpace(rawId))
        {
            if (!Guid.TryParse(rawId, out var id))
            {
                return (null, Format(InvalidIdMessage, rawId));
            }

            var byId = macros.FirstOrDefault(m => m.Id == id);
            return byId != null ? (byId, null) : (null, Format(IdNotFoundMessage, rawId));
        }

        var sourceName = GetParameter<string>(parameters, MacroNameParameter);
        return string.IsNullOrWhiteSpace(sourceName)
            ? (null, SourceMissingMessage)
            : MacroResolver.Resolve(macros, sourceName);
    }

    private async Task<SkillResult> StoreCopyAsync(
        MacroResource source,
        string copyName,
        string combined,
        string? description,
        MacroRegressionResult regression,
        CancellationToken cancellationToken)
    {
        var resource = new MacroResource
        {
            Name = copyName,
            Content = combined,
            Type = (int)MacroFunctionEnum.Custom,
            Category = MacroCategoryEnum.Unspecified,
            Description = MacroDescriptionFactory.ForAllCoreLanguages(description)
        };

        MacroResource? created;
        try
        {
            created = await _mediator.Send(new PostCommand(resource, MacroOrigin.AssistantExtension), cancellationToken);
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        if (created == null)
        {
            return SkillResult.Error(Format(NotCreatedMessage, copyName));
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Name, SourceMacroId = source.Id, regression.ComparedSamples },
            Format(CreatedMessage, created.Name, source.Name, created.Id, regression.ComparedSamples, regression.SkippedSamples));
    }

    private static string DescribeRegressionFailure(MacroResource source, MacroRegressionResult regression)
    {
        if (regression.FailureMessage != null)
        {
            return Format(
                regression.FailureKind == MacroRegressionFailureKind.CopyRuntimeError
                    ? CopyRuntimeFailedMessage
                    : RegressionAbortedMessage,
                regression.FailureMessage);
        }

        var deviations = regression.Deviations.Select(d => Format(
            DeviationFormat,
            d.SampleDescription,
            d.Channel,
            DescribeValue(d.OriginalValue),
            DescribeValue(d.CopyValue)) + (d.AcceptedTotal.HasValue ? Format(AcceptedTotalFormat, d.AcceptedTotal.Value) : string.Empty));

        return Format(
            RegressionFailedMessage,
            source.Name,
            regression.TotalDeviationCount,
            regression.Deviations.Count,
            string.Join(DeviationSeparator, deviations));
    }

    private static string DescribeValue(decimal? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : MissingValueText;

    private static string Format(string format, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, format, args);
}
