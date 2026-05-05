using AutoMapper;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<RegisterDto, AppUser>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.NormalizedUserName, opt => opt.MapFrom(s => s.Email.ToUpperInvariant()))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.NormalizedEmail, opt => opt.MapFrom(s => s.Email.ToUpperInvariant()))
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FullName));
    }
}
