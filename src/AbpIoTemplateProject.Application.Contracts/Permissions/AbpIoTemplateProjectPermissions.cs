namespace AbpIoTemplateProject.Permissions;

public static class AbpIoTemplateProjectPermissions
{
    public const string GroupName = "Education";

    public static class Courses
    {
        public const string Default = GroupName + ".Courses";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Teachers
    {
        public const string Default = GroupName + ".Teachers";
        public const string Create = Default + ".Create";
        public const string Update = Default + ".Update";
        public const string Delete = Default + ".Delete";
    }

    public static class Classes
    {
        public const string Default = GroupName + ".Classes";
        public const string Manage = Default + ".Manage";
    }

    public static class Students
    {
        public const string Default = GroupName + ".Students";
        public const string Manage = Default + ".Manage";
    }

    public static class Enrollments
    {
        public const string Default = GroupName + ".Enrollments";
        public const string Manage = Default + ".Manage";
    }

    public static class Leads
    {
        public const string Default = GroupName + ".Leads";
        public const string Manage = Default + ".Manage";
    }

    public static class PlacementTests
    {
        public const string Default = GroupName + ".PlacementTests";
        public const string Manage = Default + ".Manage";
    }

    public static class Content
    {
        public const string Default = GroupName + ".Content";
        public const string Manage = Default + ".Manage";
    }
}
