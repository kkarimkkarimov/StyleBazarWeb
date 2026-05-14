using AutoMapper;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Contact;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.DTOs.Stories;
using Stylebazar.BLL.DTOs.Wishlist;
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

        // Products
        CreateMap<ProductImage, ProductImageDto>();
        CreateMap<ProductSize, ProductSizeDto>();
        CreateMap<CreateProductSizeDto, ProductSize>();
        CreateMap<ProductColor, ProductColorDto>();

        CreateMap<Product, ProductListDto>()
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.Store != null ? s.Store.Name : string.Empty))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s =>
                s.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                ?? s.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault()));

        CreateMap<Product, ProductDetailDto>()
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.Store != null ? s.Store.Name : string.Empty))
            .ForMember(d => d.StoreSlug, opt => opt.MapFrom(s => s.Store != null ? s.Store.Slug : string.Empty))
            .ForMember(d => d.StorePhone, opt => opt.MapFrom(s => s.Store != null ? s.Store.ContactPhone : null))
            .ForMember(d => d.StoreWhatsApp, opt => opt.MapFrom(s => s.Store != null ? s.Store.WhatsAppNumber : null))
            .ForMember(d => d.StoreInstagram, opt => opt.MapFrom(s => s.Store != null ? s.Store.InstagramHandle : null))
            .ForMember(d => d.StoreTelegram, opt => opt.MapFrom(s => s.Store != null ? s.Store.TelegramHandle : null))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.CategorySlug, opt => opt.MapFrom(s => s.Category != null ? s.Category.Slug : string.Empty))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images.OrderBy(i => i.DisplayOrder)))
            .ForMember(d => d.Sizes, opt => opt.MapFrom(s => s.Sizes))
            .ForMember(d => d.Colors, opt => opt.MapFrom(s => s.Colors.OrderBy(c => c.DisplayOrder)))
            .ForMember(d => d.ReviewCount, opt => opt.MapFrom(s => s.Reviews.Count))
            .ForMember(d => d.AverageRating, opt => opt.MapFrom(s => s.Reviews.Any() ? s.Reviews.Average(r => r.Rating) : 0d));

        // Wishlist
        CreateMap<Stylebazar.DAL.Entities.Wishlist, WishlistItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForMember(d => d.ProductSlug, opt => opt.MapFrom(s => s.Product != null ? s.Product.Slug : string.Empty))
            .ForMember(d => d.Price, opt => opt.MapFrom(s => s.Product != null ? s.Product.Price : 0m))
            .ForMember(d => d.DiscountPrice, opt => opt.MapFrom(s => s.Product != null ? s.Product.DiscountPrice : null))
            .ForMember(d => d.MainImageUrl, opt => opt.MapFrom(s =>
                s.Product != null
                    ? (s.Product.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                       ?? s.Product.Images.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault())
                    : null))
            .ForMember(d => d.StoreName, opt => opt.MapFrom(s => s.Product != null && s.Product.Store != null ? s.Product.Store.Name : string.Empty))
            .ForMember(d => d.AddedAt, opt => opt.MapFrom(s => s.CreatedDate));

        // Reviews
        CreateMap<ReviewImage, ReviewImageDto>();
        CreateMap<Review, ReviewDto>()
            .ForMember(d => d.UserFullName, opt => opt.MapFrom(s => s.User != null ? s.User.FullName : string.Empty))
            .ForMember(d => d.UserProfileImageUrl, opt => opt.MapFrom(s => s.User != null ? s.User.ProfileImageUrl : null))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images));

        // Stories
        CreateMap<Story, StoryDto>();

        // Hero banners
        CreateMap<HeroBanner, HeroBannerDto>();
        CreateMap<CreateHeroBannerDto, HeroBanner>()
            .ForMember(d => d.Id, opt => opt.Ignore())
            .ForMember(d => d.IsActive, opt => opt.MapFrom(_ => true));

        // Contact
        CreateMap<ContactMessage, ContactMessageDto>();

        // Newsletter
        CreateMap<NewsletterSubscriber, NewsletterSubscriberDto>();
    }
}
