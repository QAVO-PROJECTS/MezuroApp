namespace MezuroApp.Domain.HelperEntities
{
    public static class Permissions
    {
        public const string ClaimType = "permission";

        public static class Products
        {
            public const string Create  = "Products.Create";
            public const string Update  = "Products.Update";
            public const string Delete  = "Products.Delete";
            public const string Read    = "Products.Read";

            public const string SetActive   = "Products.SetActive";
            public const string SetFeatured = "Products.SetFeatured";
            public const string SetNew      = "Products.SetNew";
            public const string SetSale     = "Products.SetSale";
        }
        
        public static class Coupons
        {
            public const string Create = "Coupons.Create";
            public const string Update = "Coupons.Update";
            public const string Delete = "Coupons.Delete";
            public const string Read   = "Coupons.Read";
            public const string SetActive = "Coupons.SetActive"; // istəsən ayrıca policy də istifadə edə bilərsən
        }


        public static class Categories
        {
            public const string Create = "Categories.Create";
            public const string Update = "Categories.Update";
            public const string Delete = "Categories.Delete";
            public const string Read   = "Categories.Read";
        }

        public static class ProductColors
        {
            public const string Create = "ProductColors.Create";
            public const string Update = "ProductColors.Update";
            public const string Delete = "ProductColors.Delete";
            public const string Read   = "ProductColors.Read";
        }
        public static class Options
        {
            public const string Create = "Options.Create";
            public const string Update = "Options.Update";
            public const string Delete = "Options.Delete";

        }
        public static class Reviews
        {
            public const string Create      = "Reviews.Create";
            public const string Update      = "Reviews.Update";
            public const string Delete      = "Reviews.Delete";
            public const string GetAll        = "Reviews.GetAll";

            public const string SetStatus   = "Reviews.SetStatus";   // Status dəyişmək
            public const string Reply       = "Reviews.Reply";       // Admin cavabı
            public const string Like        = "Reviews.Like";        // Like
            public const string Dislike     = "Reviews.Dislike";     // Dislike
            public const string Sort        = "Reviews.Sort";        // Sort etmək
        }
        public static class ProductOptions
        {
            public const string Create = "ProductOptions.Create";
            public const string Update = "ProductOptions.Update";
            public const string Delete = "ProductOptions.Delete";

        }
        public static class ProductVariants
        {
            public const string Create = "ProductVariants.Create";
            public const string Update = "ProductVariants.Update";
            public const string Delete = "ProductVariants.Delete";
            public const string Read   = "ProductVariants.Read";
        }

        public static class Admins
        {
            public const string Manage = "Admins.Manage";
        }

        public static IEnumerable<string> All()
        {
            // PRODUCTS
            yield return Products.Create;
            yield return Products.Update;
            yield return Products.Delete;
            yield return Products.Read;
            yield return Products.SetActive;
            yield return Products.SetFeatured;
            yield return Products.SetNew;
            yield return Products.SetSale;
            
            yield return Coupons.Create;
            yield return Coupons.Update;
            yield return Coupons.Delete;
            yield return Coupons.Read;
            yield return Coupons.SetActive;


            // CATEGORIES
            yield return Categories.Create;
            yield return Categories.Update;
            yield return Categories.Delete;
            yield return Categories.Read;

            // OPTIONS
            yield return Options.Create;
            yield return Options.Update;
            yield return Options.Delete;
            
            //Product Options
            yield return ProductOptions.Create;
            yield return ProductOptions.Update;
            yield return ProductOptions.Delete;
      
            // PRODUCT COLORS
            yield return ProductColors.Create;
            yield return ProductColors.Update;
            yield return ProductColors.Delete;
            yield return ProductColors.Read;

            // PRODUCT VARIANTS
            yield return ProductVariants.Create;
            yield return ProductVariants.Update;
            yield return ProductVariants.Delete;
            yield return ProductVariants.Read;

            // ADMINS
            yield return Admins.Manage;
            // REVIEWS
            yield return Reviews.Create;
            yield return Reviews.Update;
            yield return Reviews.Delete;
            yield return Reviews.GetAll;
            yield return Reviews.SetStatus;
            yield return Reviews.Reply;
            yield return Reviews.Like;
            yield return Reviews.Dislike;
            yield return Reviews.Sort;
        }
    }
}