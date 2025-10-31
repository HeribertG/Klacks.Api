using AutoMapper;
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

namespace Klacks.Api.Application.AutoMapper;

public class SettingsMappingProfile : Profile
{
    public SettingsMappingProfile()
    {
        CreateMap<Annotation, AnnotationResource>();

        CreateMap<AnnotationResource, Annotation>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<Countries, CountryResource>()
            .ForMember(dest => dest.Select, opt => opt.Ignore());

        CreateMap<CountryResource, Countries>()
            .IgnoreAuditFields();

        CreateMap<Macro, MacroResource>();

        CreateMap<MacroResource, Macro>()
            .IgnoreAuditFields();

        CreateMap<TruncatedAbsence, TruncatedAbsence>();

        CreateMap<Absence, AbsenceResource>();

        CreateMap<AbsenceResource, Absence>()
            .IgnoreAuditFields();

        CreateMap<State, StateResource>();

        CreateMap<StateResource, State>()
            .IgnoreAuditFields();

        CreateMap<BreakReason, BreakReasonResource>();

        CreateMap<BreakReasonResource, BreakReason>()
            .IgnoreAuditFields();

        CreateMap<History, HistoryResource>();

        CreateMap<HistoryResource, History>()
            .IgnoreAuditFields()
            .ForMember(dest => dest.Client, opt => opt.Ignore());

        CreateMap<MacroType, MacroTypeResource>().ReverseMap();
        CreateMap<Klacks.Api.Domain.Models.Settings.Settings, SettingsResource>().ReverseMap();

        CreateMap<LastChangeMetaData, LastChangeMetaDataResource>()
            .ForMember(dest => dest.Autor, opt => opt.MapFrom(src => src.Author));
    }
}