using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Chat;
using Stylebazar.BLL.DTOs.Contact;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.DTOs.Newsletter;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.DTOs.Stories;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.BLL.Services.Chat;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.MVC.Areas.Admin.ViewModels;

/// <summary>
/// Dashboard home — six KPI cards + three recent-activity lists.
/// </summary>
public class AdminDashboardViewModel
{
    public int TotalUsers { get; set; }
    public int TotalSellers { get; set; }
    public int TotalStores { get; set; }
    public int TotalProducts { get; set; }
    public int ActiveStories { get; set; }
    public int UnreadContactMessages { get; set; }

    public List<UserAdminDto> RecentUsers { get; set; } = new();
    public List<ContactMessageDto> RecentMessages { get; set; } = new();
    public List<AdminLoginEventDto> RecentLoginEvents { get; set; } = new();
}

public class AdminUsersPageViewModel
{
    public Paginate<UserAdminDto> Users { get; set; } = new();
    public UserAdminListFilter Filter { get; set; } = new();
}

public class AdminUserDetailViewModel
{
    public UserAdminDto User { get; set; } = new();
    public List<LoginEventDto> LoginHistory { get; set; } = new();
    public List<StoreListDto> Stores { get; set; } = new();

    /// <summary>Resolved current subscription (synthesized Free when no history).</summary>
    public SubscriptionDto CurrentSubscription { get; set; } = new();

    /// <summary>Most-recent 10 subscription change rows for the audit table.</summary>
    public List<SubscriptionDto> SubscriptionHistory { get; set; } = new();
}

public class AdminStoresPageViewModel
{
    public Paginate<StoreListDto> Stores { get; set; } = new();
    public string? Search { get; set; }
    public bool? IsActive { get; set; }
    public int Page { get; set; } = 1;
}

public class AdminProductsPageViewModel
{
    public Paginate<ProductListDto> Products { get; set; } = new();
    public List<CategoryListDto> Categories { get; set; } = new();
    public List<StoreListDto> Stores { get; set; } = new();
    public ProductListQueryDto Query { get; set; } = new();
}

public class AdminCategoriesPageViewModel
{
    public List<CategoryListDto> Categories { get; set; } = new();
}

public class AdminReviewsPageViewModel
{
    public List<ReviewAdminRow> Reviews { get; set; } = new();
    public int? RatingFilter { get; set; }
    public string? Search { get; set; }
}

public class ReviewAdminRow
{
    public ReviewDto Review { get; set; } = new();
    public string ProductName { get; set; } = string.Empty;
    public int ProductId { get; set; }
}

public class AdminContactMessagesPageViewModel
{
    public Paginate<ContactMessageDto> Messages { get; set; } = new();
    public ContactStatus? StatusFilter { get; set; }
    public int Page { get; set; } = 1;
}

public class AdminNewsletterPageViewModel
{
    public Paginate<NewsletterSubscriberDto> Subscribers { get; set; } = new();
    public bool? ActiveFilter { get; set; }
    public int Page { get; set; } = 1;
}

public class AdminHeroBannersPageViewModel
{
    public List<HeroBannerDto> Banners { get; set; } = new();
}

public class AdminStoriesPageViewModel
{
    public List<StoryAdminRow> Stories { get; set; } = new();
}

public class StoryAdminRow
{
    public StoryDto Story { get; set; } = new();
    public string StoreName { get; set; } = string.Empty;
    public string StoreSlug { get; set; } = string.Empty;
}

public class AdminConversationsPageViewModel
{
    public Paginate<AdminConversationRowDto> Conversations { get; set; } = new();
    public int Page { get; set; } = 1;
}

public class AdminConversationDetailViewModel
{
    public AdminConversationRowDto Conversation { get; set; } = new();
    public Paginate<MessageDto> Messages { get; set; } = new();
    public int Page { get; set; } = 1;
}
