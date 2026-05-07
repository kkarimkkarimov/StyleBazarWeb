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
    }
}
