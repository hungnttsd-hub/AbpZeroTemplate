using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;
using AbpIoTemplateProject.Education;

namespace AbpIoTemplateProject.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ReplaceDbContext(typeof(ISettingManagementDbContext))]
[ConnectionStringName("Default")]
public class AbpIoTemplateProjectDbContext :
    AbpDbContext<AbpIoTemplateProjectDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext,
    ISettingManagementDbContext
{

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    //Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }
    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }
    public DbSet<Setting> Settings { get; set; }
    public DbSet<SettingDefinitionRecord> SettingDefinitionRecords { get; set; }

    // Education
    public DbSet<CourseCategory> CourseCategories { get; set; }
    public DbSet<CourseLevel> CourseLevels { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<CourseTeacher> CourseTeachers { get; set; }
    public DbSet<CourseBenefit> CourseBenefits { get; set; }
    public DbSet<CourseFaq> CourseFaqs { get; set; }
    public DbSet<CourseModule> CourseModules { get; set; }
    public DbSet<CourseLesson> CourseLessons { get; set; }
    public DbSet<LearningPath> LearningPaths { get; set; }
    public DbSet<LearningPathStep> LearningPathSteps { get; set; }
    public DbSet<LearningPathCourse> LearningPathCourses { get; set; }
    public DbSet<Teacher> Teachers { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<Lead> Leads { get; set; }
    public DbSet<Campus> Campuses { get; set; }
    public DbSet<CourseClass> CourseClasses { get; set; }
    public DbSet<Enrollment> Enrollments { get; set; }
    public DbSet<PlacementTest> PlacementTests { get; set; }
    public DbSet<PlacementQuestion> PlacementQuestions { get; set; }
    public DbSet<PlacementAttempt> PlacementAttempts { get; set; }
    public DbSet<PlacementAnswer> PlacementAnswers { get; set; }
    public DbSet<ArticleCategory> ArticleCategories { get; set; }
    public DbSet<Article> Articles { get; set; }
    public DbSet<LearningDocument> LearningDocuments { get; set; }
    public DbSet<StudentAchievement> StudentAchievements { get; set; }
    public DbSet<Banner> Banners { get; set; }
    public DbSet<SiteSetting> SiteSettings { get; set; }
    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<NotificationMessage> NotificationMessages { get; set; }

    #endregion

    public AbpIoTemplateProjectDbContext(DbContextOptions<AbpIoTemplateProjectDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureFeatureManagement();
        builder.ConfigureTenantManagement();
        builder.ConfigureEducation();

    }
}
