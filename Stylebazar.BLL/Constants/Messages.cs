namespace Stylebazar.BLL.Constants;

public static class Messages
{
    public static class Category
    {
        public const string NotFound = "Category was not found.";
        public const string SlugAlreadyExists = "A category with this slug already exists.";
        public const string ParentNotFound = "Parent category was not found.";
        public const string CannotBeOwnParent = "A category cannot be its own parent.";
        public const string CircularReference = "This parent assignment would create a circular reference.";
        public const string HasChildren = "Category has child categories. Move or delete them first.";
        public const string HasProducts = "Category has products. Reassign or delete them first.";
    }

    public static class Store
    {
        public const string NotFound = "Mağaza tapılmadı.";
        public const string SlugAlreadyExists = "Bu slug ilə mağaza artıq mövcuddur.";
        public const string OwnerNotAuthenticated = "Bu əməliyyat üçün hesabınıza daxil olun.";
        public const string OwnerMustBeSeller = "Yalnız satıcılar mağaza yarada bilər.";
        public const string NotOwner = "Bu mağazanın sahibi siz deyilsiniz.";
        public const string HasProducts = "Mağazada məhsullar var. Əvvəlcə onları silin və ya başqa mağazaya köçürün.";
        public const string Inactive = "Bu mağaza deaktivdir və yeni məhsul qəbul edə bilmir.";
        public const string ImageRequired = "Şəkil seçilməyib.";
    }

    public static class Product
    {
        public const string NotFound = "Məhsul tapılmadı.";
        public const string ImageNotFound = "Məhsul şəkli tapılmadı.";
        public const string SizeNotFound = "Məhsul ölçüsü tapılmadı.";
        public const string SlugAlreadyExists = "Bu slug ilə məhsul artıq mövcuddur.";
        public const string DiscountTooHigh = "Endirimli qiymət əsas qiymətdən kiçik olmalıdır.";
        public const string NotStoreOwner = "Yalnız öz mağazanızın məhsullarını idarə edə bilərsiniz.";
        public const string NoStoreYet = "Məhsul əlavə etməzdən əvvəl bir mağaza yaratmalısınız.";
        public const string ImageRequired = "Şəkil seçilməyib.";
    }

    public static class Wishlist
    {
        public const string NotAuthenticated = "You must be signed in to manage your wishlist.";
        public const string ProductAlreadyInList = "This product is already in your wishlist.";
        public const string ItemNotFound = "This product is not in your wishlist.";
    }

    public static class Review
    {
        public const string NotFound = "Review was not found.";
        public const string ImageNotFound = "Review image was not found.";
        public const string NotAuthenticated = "You must be signed in to review products.";
        public const string AlreadyReviewed = "You have already reviewed this product.";
        public const string NotOwner = "You can only modify your own reviews.";
    }

    public static class Story
    {
        public const string NotFound = "Story was not found.";
        public const string NotAuthenticated = "You must be signed in to publish a story.";
        public const string NotStoreOwner = "You can only publish stories for stores you own.";
        public const string TooManyActive = "This store already has the maximum number of active stories.";
    }

    public static class HeroBanner
    {
        public const string NotFound = "Hero banner was not found.";
    }

    public static class Contact
    {
        public const string NotFound = "Contact message was not found.";
    }

    public static class Newsletter
    {
        public const string NotFound = "Subscriber was not found.";
        public const string AlreadySubscribed = "This email is already subscribed.";
    }

    public static class Account
    {
        public const string NotAuthenticated = "Bu əməliyyat üçün hesabınıza daxil olun.";
        public const string NotFound = "İstifadəçi tapılmadı.";
        public const string WrongCurrentPassword = "Mövcud parol səhvdir.";
        public const string AvatarRequired = "Şəkil seçilməyib.";
    }

    public static class Address
    {
        public const string NotAuthenticated = "Ünvanlarınızı idarə etmək üçün hesabınıza daxil olun.";
        public const string NotFound = "Ünvan tapılmadı.";
        public const string NotOwner = "Yalnız öz ünvanlarınızı dəyişə bilərsiniz.";
        public const string MaxLimitReached = "Bir hesab üçün maksimum 10 ünvan saxlana bilər.";
    }

    public static class StoreFollower
    {
        public const string NotAuthenticated = "Mağazanı izləmək üçün hesabınıza daxil olun.";
    }
}
