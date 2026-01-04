using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Histories;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Histories;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Presentation.DTOs.Staffs;
using Riok.Mapperly.Abstractions;

namespace Klacks.Api.Application.Mappers;

[Mapper]
public partial class SettingsMapper
{
    public partial AnnotationResource ToAnnotationResource(Annotation annotation);
    public partial List<AnnotationResource> ToAnnotationResources(List<Annotation> annotations);

    [MapperIgnoreTarget(nameof(Annotation.CreateTime))]
    [MapperIgnoreTarget(nameof(Annotation.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Annotation.UpdateTime))]
    [MapperIgnoreTarget(nameof(Annotation.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Annotation.DeletedTime))]
    [MapperIgnoreTarget(nameof(Annotation.IsDeleted))]
    [MapperIgnoreTarget(nameof(Annotation.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Annotation.Client))]
    public partial Annotation ToAnnotationEntity(AnnotationResource resource);

    [MapperIgnoreTarget(nameof(Annotation.Id))]
    [MapperIgnoreTarget(nameof(Annotation.CreateTime))]
    [MapperIgnoreTarget(nameof(Annotation.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Annotation.UpdateTime))]
    [MapperIgnoreTarget(nameof(Annotation.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Annotation.DeletedTime))]
    [MapperIgnoreTarget(nameof(Annotation.IsDeleted))]
    [MapperIgnoreTarget(nameof(Annotation.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(Annotation.Client))]
    public partial void UpdateAnnotationEntity(AnnotationResource resource, Annotation target);

    [MapperIgnoreTarget(nameof(CountryResource.Select))]
    public partial CountryResource ToCountryResource(Countries country);
    public partial List<CountryResource> ToCountryResources(List<Countries> countries);

    [MapperIgnoreTarget(nameof(Countries.CreateTime))]
    [MapperIgnoreTarget(nameof(Countries.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Countries.UpdateTime))]
    [MapperIgnoreTarget(nameof(Countries.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Countries.DeletedTime))]
    [MapperIgnoreTarget(nameof(Countries.IsDeleted))]
    [MapperIgnoreTarget(nameof(Countries.CurrentUserDeleted))]
    public partial Countries ToCountryEntity(CountryResource resource);

    [MapperIgnoreTarget(nameof(Countries.Id))]
    [MapperIgnoreTarget(nameof(Countries.CreateTime))]
    [MapperIgnoreTarget(nameof(Countries.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Countries.UpdateTime))]
    [MapperIgnoreTarget(nameof(Countries.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Countries.DeletedTime))]
    [MapperIgnoreTarget(nameof(Countries.IsDeleted))]
    [MapperIgnoreTarget(nameof(Countries.CurrentUserDeleted))]
    public partial void UpdateCountryEntity(CountryResource resource, Countries target);

    public partial MacroResource ToMacroResource(Macro macro);
    public partial List<MacroResource> ToMacroResources(List<Macro> macros);

    [MapperIgnoreTarget(nameof(Macro.CreateTime))]
    [MapperIgnoreTarget(nameof(Macro.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Macro.UpdateTime))]
    [MapperIgnoreTarget(nameof(Macro.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Macro.DeletedTime))]
    [MapperIgnoreTarget(nameof(Macro.IsDeleted))]
    [MapperIgnoreTarget(nameof(Macro.CurrentUserDeleted))]
    public partial Macro ToMacroEntity(MacroResource resource);

    public partial TruncatedAbsence CloneTruncatedAbsence(TruncatedAbsence absence);

    public partial AbsenceResource ToAbsenceResource(Absence absence);
    public partial List<AbsenceResource> ToAbsenceResources(List<Absence> absences);

    [MapperIgnoreTarget(nameof(Absence.CreateTime))]
    [MapperIgnoreTarget(nameof(Absence.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Absence.UpdateTime))]
    [MapperIgnoreTarget(nameof(Absence.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Absence.DeletedTime))]
    [MapperIgnoreTarget(nameof(Absence.IsDeleted))]
    [MapperIgnoreTarget(nameof(Absence.CurrentUserDeleted))]
    public partial Absence ToAbsenceEntity(AbsenceResource resource);

    [MapperIgnoreTarget(nameof(Absence.Id))]
    [MapperIgnoreTarget(nameof(Absence.CreateTime))]
    [MapperIgnoreTarget(nameof(Absence.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(Absence.UpdateTime))]
    [MapperIgnoreTarget(nameof(Absence.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(Absence.DeletedTime))]
    [MapperIgnoreTarget(nameof(Absence.IsDeleted))]
    [MapperIgnoreTarget(nameof(Absence.CurrentUserDeleted))]
    public partial void UpdateAbsenceEntity(AbsenceResource resource, Absence target);

    public partial StateResource ToStateResource(State state);
    public partial List<StateResource> ToStateResources(List<State> states);

    [MapperIgnoreTarget(nameof(State.CreateTime))]
    [MapperIgnoreTarget(nameof(State.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(State.UpdateTime))]
    [MapperIgnoreTarget(nameof(State.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(State.DeletedTime))]
    [MapperIgnoreTarget(nameof(State.IsDeleted))]
    [MapperIgnoreTarget(nameof(State.CurrentUserDeleted))]
    public partial State ToStateEntity(StateResource resource);

    [MapperIgnoreTarget(nameof(State.Id))]
    [MapperIgnoreTarget(nameof(State.CreateTime))]
    [MapperIgnoreTarget(nameof(State.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(State.UpdateTime))]
    [MapperIgnoreTarget(nameof(State.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(State.DeletedTime))]
    [MapperIgnoreTarget(nameof(State.IsDeleted))]
    [MapperIgnoreTarget(nameof(State.CurrentUserDeleted))]
    public partial void UpdateStateEntity(StateResource resource, State target);

    public partial BreakReasonResource ToBreakReasonResource(BreakReason reason);
    public partial List<BreakReasonResource> ToBreakReasonResources(List<BreakReason> reasons);

    [MapperIgnoreTarget(nameof(BreakReason.CreateTime))]
    [MapperIgnoreTarget(nameof(BreakReason.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(BreakReason.UpdateTime))]
    [MapperIgnoreTarget(nameof(BreakReason.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(BreakReason.DeletedTime))]
    [MapperIgnoreTarget(nameof(BreakReason.IsDeleted))]
    [MapperIgnoreTarget(nameof(BreakReason.CurrentUserDeleted))]
    public partial BreakReason ToBreakReasonEntity(BreakReasonResource resource);

    public partial HistoryResource ToHistoryResource(History history);
    public partial List<HistoryResource> ToHistoryResources(List<History> histories);

    [MapperIgnoreTarget(nameof(History.CreateTime))]
    [MapperIgnoreTarget(nameof(History.CurrentUserCreated))]
    [MapperIgnoreTarget(nameof(History.UpdateTime))]
    [MapperIgnoreTarget(nameof(History.CurrentUserUpdated))]
    [MapperIgnoreTarget(nameof(History.DeletedTime))]
    [MapperIgnoreTarget(nameof(History.IsDeleted))]
    [MapperIgnoreTarget(nameof(History.CurrentUserDeleted))]
    [MapperIgnoreTarget(nameof(History.Client))]
    public partial History ToHistoryEntity(HistoryResource resource);

    public partial MacroTypeResource ToMacroTypeResource(MacroType macroType);
    public partial MacroType ToMacroTypeEntity(MacroTypeResource resource);

    public partial SettingsResource ToSettingsResource(Domain.Models.Settings.Settings settings);
    public partial Domain.Models.Settings.Settings ToSettingsEntity(SettingsResource resource);

    public LastChangeMetaDataResource ToLastChangeMetaDataResource(LastChangeMetaData metaData)
    {
        return new LastChangeMetaDataResource
        {
            LastChangesDate = metaData.LastChangesDate,
            Autor = metaData.Author
        };
    }
}
