namespace AbpIoTemplateProject.Permissions;

public static class AbpIoTemplateProjectPermissions
{
    public const string GroupName = "Store";

    public static class Products
    {
        public const string Default = GroupName + ".Products";
        public const string View = Default + ".View";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
        public const string ManageImages = Default + ".ManageImages";
        public const string ManagePrice = Default + ".ManagePrice";
    }

    public static class Categories
    {
        public const string Default = GroupName + ".Categories";
    }

    public static class Brands
    {
        public const string Default = GroupName + ".Brands";
    }

    public static class Suppliers
    {
        public const string Default = GroupName + ".Suppliers";
    }

    public static class Inventory
    {
        public const string Default = GroupName + ".Inventory";
        public const string View = Default + ".View";
        public const string Receive = Default + ".Receive";
        public const string Issue = Default + ".Issue";
        public const string Adjust = Default + ".Adjust";
        public const string Transfer = Default + ".Transfer";
    }

    public static class Customers
    {
        public const string Default = GroupName + ".Customers";
        public const string View = Default + ".View";
        public const string Update = Default + ".Update";
    }

    public static class Orders
    {
        public const string Default = GroupName + ".Orders";
        public const string View = Default + ".View";
        public const string Confirm = Default + ".Confirm";
        public const string Prepare = Default + ".Prepare";
        public const string Ship = Default + ".Ship";
        public const string Complete = Default + ".Complete";
        public const string Cancel = Default + ".Cancel";
        public const string Return = Default + ".Return";
    }

    public static class Payments
    {
        public const string Default = GroupName + ".Payments";
        public const string View = Default + ".View";
        public const string Confirm = Default + ".Confirm";
        public const string Refund = Default + ".Refund";
    }

    public static class Promotions
    {
        public const string Default = GroupName + ".Promotions";
    }

    public static class Banners
    {
        public const string Default = GroupName + ".Banners";
    }

    public static class Articles
    {
        public const string Default = GroupName + ".Articles";
    }

    public static class Settings
    {
        public const string Default = GroupName + ".Settings";
    }
}
