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
}
