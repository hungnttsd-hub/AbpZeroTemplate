namespace WebHoanTien.Permissions;

public static class WebHoanTienPermissions
{
    public const string GroupName = "Affiliate";

    public static class Admin
    {
        public const string Default = GroupName + ".Admin";
        public const string Settings = Default + ".Settings";
        public const string CommissionRules = Default + ".CommissionRules";
        public const string Orders = Default + ".Orders";
        public const string Sync = Default + ".Sync";
        public const string ManualMatch = Default + ".ManualMatch";
        public const string Payouts = Default + ".Payouts";
    }
}
