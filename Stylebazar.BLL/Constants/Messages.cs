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
        public const string NotFound = "Store was not found.";
        public const string SlugAlreadyExists = "A store with this slug already exists.";
        public const string OwnerNotAuthenticated = "You must be signed in to perform this action.";
        public const string OwnerMustBeSeller = "Only sellers can own a store.";
        public const string NotOwner = "You are not the owner of this store.";
        public const string HasProducts = "Store has products. Remove or reassign them first.";
        public const string Inactive = "This store is inactive and cannot accept new products.";
    }

    public static class Product
    {
        public const string NotFound = "Product was not found.";
        public const string ImageNotFound = "Product image was not found.";
        public const string SizeNotFound = "Product size was not found.";
        public const string SlugAlreadyExists = "A product with this slug already exists.";
        public const string DiscountTooHigh = "Discount price must be less than price.";
        public const string NotStoreOwner = "You can only manage products in stores you own.";
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
}
