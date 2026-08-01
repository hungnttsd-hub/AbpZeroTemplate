using AbpIoTemplateProject.Education;
using Microsoft.EntityFrameworkCore;
using Volo.Abp;
using Volo.Abp.EntityFrameworkCore.Modeling;

namespace AbpIoTemplateProject.EntityFrameworkCore;

public static class EducationDbContextModelCreatingExtensions
{
    private const string Schema = "education";

    public static void ConfigureEducation(this ModelBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        builder.Entity<CourseCategory>(b => { b.ToTable("CourseCategories", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); });
        builder.Entity<CourseLevel>(b => { b.ToTable("CourseLevels", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); });
        builder.Entity<Course>(b =>
        {
            b.ToTable("Courses", Schema); b.ConfigureByConvention();
            b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => x.Slug).IsUnique();
            b.Property(x => x.TuitionFee).HasPrecision(18, 2); b.Property(x => x.PromotionalFee).HasPrecision(18, 2);
            b.HasOne<CourseCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
            b.HasOne<CourseLevel>().WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.SetNull);
        });
        builder.Entity<CourseTeacher>(b => { b.ToTable("CourseTeachers", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.CourseId, x.TeacherId }).IsUnique(); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Teacher>().WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<CourseBenefit>(b => { b.ToTable("CourseBenefits", Schema); b.ConfigureByConvention(); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<CourseFaq>(b => { b.ToTable("CourseFaqs", Schema); b.ConfigureByConvention(); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<CourseModule>(b => { b.ToTable("CourseModules", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.CourseId, x.DisplayOrder }).IsUnique(); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<CourseLesson>(b => { b.ToTable("CourseLessons", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.CourseModuleId, x.DisplayOrder }).IsUnique(); b.HasOne<CourseModule>().WithMany().HasForeignKey(x => x.CourseModuleId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<LearningPath>(b => { b.ToTable("LearningPaths", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => x.Slug).IsUnique(); });
        builder.Entity<LearningPathStep>(b => { b.ToTable("LearningPathSteps", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.LearningPathId, x.DisplayOrder }).IsUnique(); b.HasOne<LearningPath>().WithMany().HasForeignKey(x => x.LearningPathId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<LearningPathCourse>(b => { b.ToTable("LearningPathCourses", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.LearningPathId, x.CourseId }).IsUnique(); b.HasOne<LearningPath>().WithMany().HasForeignKey(x => x.LearningPathId).OnDelete(DeleteBehavior.Cascade); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict); });
        builder.Entity<Teacher>(b => { b.ToTable("Teachers", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); });
        builder.Entity<Student>(b => { b.ToTable("Students", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.IdentityUserId).IsUnique().HasFilter("[IdentityUserId] IS NOT NULL"); });
        builder.Entity<Lead>(b => { b.ToTable("Leads", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.Status, x.CreationTime }); b.HasOne<Course>().WithMany().HasForeignKey(x => x.InterestedCourseId).OnDelete(DeleteBehavior.SetNull); });
        builder.Entity<Campus>(b => { b.ToTable("Campuses", Schema); b.ConfigureByConvention(); });
        builder.Entity<CourseClass>(b => { b.ToTable("CourseClasses", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Code).IsUnique(); b.HasIndex(x => new { x.Status, x.StartDate }); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Campus>().WithMany().HasForeignKey(x => x.CampusId).OnDelete(DeleteBehavior.SetNull); b.HasOne<Teacher>().WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.SetNull); });
        builder.Entity<Enrollment>(b => { b.ToTable("Enrollments", Schema); b.ConfigureByConvention(); b.Property(x => x.AgreedTuitionFee).HasPrecision(18, 2); b.Property(x => x.PaidAmount).HasPrecision(18, 2); b.HasIndex(x => new { x.StudentId, x.CourseClassId }).IsUnique(); b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict); b.HasOne<CourseClass>().WithMany().HasForeignKey(x => x.CourseClassId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<PlacementTest>(b => { b.ToTable("PlacementTests", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); });
        builder.Entity<PlacementQuestion>(b => { b.ToTable("PlacementQuestions", Schema); b.ConfigureByConvention(); b.Property(x => x.Score).HasPrecision(9, 2); b.HasOne<PlacementTest>().WithMany().HasForeignKey(x => x.PlacementTestId).OnDelete(DeleteBehavior.Cascade); });
        builder.Entity<PlacementAttempt>(b => { b.ToTable("PlacementAttempts", Schema); b.ConfigureByConvention(); b.Property(x => x.Score).HasPrecision(9, 2); b.HasOne<PlacementTest>().WithMany().HasForeignKey(x => x.PlacementTestId).OnDelete(DeleteBehavior.Restrict); b.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.SetNull); });
        builder.Entity<PlacementAnswer>(b => { b.ToTable("PlacementAnswers", Schema); b.ConfigureByConvention(); b.Property(x => x.AwardedScore).HasPrecision(9, 2); b.HasIndex(x => new { x.PlacementAttemptId, x.PlacementQuestionId }).IsUnique(); b.HasOne<PlacementAttempt>().WithMany().HasForeignKey(x => x.PlacementAttemptId).OnDelete(DeleteBehavior.Cascade); b.HasOne<PlacementQuestion>().WithMany().HasForeignKey(x => x.PlacementQuestionId).OnDelete(DeleteBehavior.Restrict); });
        builder.Entity<ArticleCategory>(b => { b.ToTable("ArticleCategories", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); });
        builder.Entity<Article>(b => { b.ToTable("Articles", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); b.HasIndex(x => new { x.Status, x.PublishedAt }); b.HasOne<ArticleCategory>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull); });
        builder.Entity<LearningDocument>(b => { b.ToTable("LearningDocuments", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Slug).IsUnique(); b.HasIndex(x => x.Status); });
        builder.Entity<StudentAchievement>(b => { b.ToTable("StudentAchievements", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.Status, x.IsFeatured }); b.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.SetNull); b.HasOne<Teacher>().WithMany().HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.SetNull); });
        builder.Entity<Banner>(b => { b.ToTable("Banners", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.Status, x.DisplayOrder }); });
        builder.Entity<SiteSetting>(b => { b.ToTable("SiteSettings", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.Key).IsUnique(); });
        builder.Entity<PaymentTransaction>(b => { b.ToTable("PaymentTransactions", Schema); b.ConfigureByConvention(); b.HasIndex(x => x.ReferenceCode).IsUnique(); b.Property(x => x.Amount).HasPrecision(18, 2); b.HasOne<Enrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Restrict); });
        builder.Entity<NotificationMessage>(b => { b.ToTable("NotificationMessages", Schema); b.ConfigureByConvention(); b.HasIndex(x => new { x.Status, x.CreationTime }); });
    }
}
