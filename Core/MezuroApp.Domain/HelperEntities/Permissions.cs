namespace MezuroApp.Domain.HelperEntities
{
    public static class Permissions
    {
        public const string ClaimType = "permission";

        public static class Products
        {
          
        
            public const string Update  = "Products.Update";
            public const string Read    = "Products.Read";

        }
        
        public static class Coupons
        {
     
            public const string Update = "Coupons.Update";

            public const string Read   = "Coupons.Read";
    
        }


        public static class Categories
        {

            public const string Update = "Categories.Update";
     
            public const string Read   = "Categories.Read";
        }

        public static class Transactions
        {
            public const string Update = "Transactions.Update";
            public const string Read     = "Transactions.Read";
        }

    
        public static class Options
        {
           
            public const string Update = "Options.Update";
            public const string Read = "Options.Read";

        }
        public static class Reviews
        {
            public const string Read      = "Reviews.Read";
            public const string Update      = "Reviews.Update";
    
        }

        public static class EmailCampaigns
        {
            public const string Update = "EmailCampaigns.Update";
            public const string Read      = "EmailCampaigns.Read";
        }
        public static class Orders
        {
            public const string Read      = "Orders.Read";
            public const string Update      = "Orders.Update";
    
        }

        public static class AbandonedCarts
        {
            public const string Update = "AbandonedCarts.Update";
            public const string Read      = "AbandonedCarts.Read";
        }

        public static class Users
        {
            public const string Read     = "Users.Read";
         
        }

        public static class Dashboard
        {
            public const string Read     = "Dashboard.Read";
       
        }
        



        public static IEnumerable<string> All()
        {
            // PRODUCTS
      
            yield return Products.Update;

            yield return Products.Read;

            
            
            yield return Coupons.Update;

            yield return Coupons.Read;



            // CATEGORIES
   
            yield return Categories.Update;
 
            yield return Categories.Read;

            // OPTIONS

            yield return Options.Update;
            yield return Options.Read;
            
         //Reviews
 
            yield return Reviews.Update;
     
            yield return Reviews.Read;
            //Orders
            yield return Orders.Update;
            yield return Orders.Read;
            
            //Email Campaigns
            yield return EmailCampaigns.Update;
            yield return EmailCampaigns.Read;
            
            //Abandoned Carts
            yield return AbandonedCarts.Update;
            yield return AbandonedCarts.Read;
            //Users

            yield return Users.Read;
            //Dashboard
            yield return Dashboard.Read;
            //Transactions
            yield return Transactions.Update;
            yield return Transactions.Read;
            
            
        }
    }
}