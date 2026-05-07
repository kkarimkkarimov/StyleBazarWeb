using AutoMapper;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.DAL.Entities;

namespace Stylebazar.BLL.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Auth
        CreateMap<RegisterDto, AppUser>()
            .ForMember(d => d.Id, opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.NormalizedUserName, opt => opt.MapFrom(s => s.Email.ToUpperInvariant()))
            .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email))
            .ForMember(d => d.NormalizedEmail, opt => opt.MapFrom(s => s.Email.ToUpperInvariant()))
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.FullName));

        // Categories
        CreateMap<Category, CategoryListDto>()
            .ForMember(d => d.ParentCategoryName, opt => opt.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null));

        CreateMap<Category, CategoryDetailDto>()
            .ForMember(d => d.ParentCategoryName, opt => opt.MapFrom(s => s.ParentCategory != null ? s.ParentCategory.Name : null))
            .ForMember(d => d.ChildCategories, opt => opt.MapFrom(s => s.ChildCategories));

        // Stores
        CreateMap<Store, StoreListDto>()
            .ForMember(d => d.ProductCount, opt => opt.MapFrom(s => s.Products.Count));

        CreateMap<Store, StoreDetailDto>()
            .ForMember(d => d.ProductCount, opt => opt.MapFrom(s => s.Products.Count))
            .ForMember(d => d.OwnerFullName, opt => opt.MapFrom(s => s.Owner != null ? s.Owner.FullName : string.Empty));
    }
}
