using Microsoft.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using WebHoanTien.WordyWings;

namespace WebHoanTien.EntityFrameworkCore;

public static class WordyWingsModelBuilderExtensions
{
    public static void ConfigureWordyWings(this ModelBuilder builder)
    {
        builder.Entity<ChildProfile>(b => {
            b.ToTable("Children", "wordy"); b.ConfigureByConvention();
            b.Property(x => x.Nickname).HasMaxLength(40).IsRequired();
            b.Property(x => x.AvatarKey).HasMaxLength(80).IsRequired();
            b.Property(x => x.AgeBand).HasMaxLength(8).IsRequired();
            b.HasIndex(x => x.ParentUserId);
        });
        builder.Entity<GameWorld>(b => {
            b.ToTable("Worlds", "wordy"); b.ConfigureByConvention(); b.Property(x => x.Id).HasMaxLength(3);
            b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        });
        builder.Entity<GameLevel>(b => {
            b.ToTable("Levels", "wordy"); b.ConfigureByConvention(); b.Property(x => x.Id).HasMaxLength(7);
            b.Property(x => x.WorldId).HasMaxLength(3).IsRequired(); b.Property(x => x.Mechanic).HasMaxLength(40).IsRequired();
            b.Property(x => x.DefinitionJson).HasColumnType("jsonb");
            b.HasOne<GameWorld>().WithMany().HasForeignKey(x => x.WorldId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.WorldId, x.DisplayOrder }).IsUnique();
        });
        builder.Entity<VocabularyTerm>(b => {
            b.ToTable("Vocabulary", "wordy"); b.ConfigureByConvention();
            b.Property(x => x.Term).HasMaxLength(80).IsRequired(); b.HasIndex(x => x.Term).IsUnique();
        });
        builder.Entity<LevelAttempt>(b => {
            b.ToTable("Attempts", "wordy"); b.ConfigureByConvention(); b.Property(x => x.LevelId).HasMaxLength(7).IsRequired();
            b.Property(x => x.PayloadJson).HasColumnType("jsonb");
            b.HasOne<ChildProfile>().WithMany().HasForeignKey(x => x.ChildProfileId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<GameLevel>().WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
            b.HasIndex(x => new { x.ChildProfileId, x.CompletedAt });
        });
        builder.Entity<PlayerProgress>(b => {
            b.ToTable("Progress", "wordy"); b.ConfigureByConvention(); b.Property(x => x.LevelId).HasMaxLength(7).IsRequired();
            b.HasIndex(x => new { x.ChildProfileId, x.LevelId }).IsUnique();
            b.HasOne<ChildProfile>().WithMany().HasForeignKey(x => x.ChildProfileId).OnDelete(DeleteBehavior.Cascade);
            b.HasOne<GameLevel>().WithMany().HasForeignKey(x => x.LevelId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<WordMastery>(b => {
            b.ToTable("Mastery", "wordy"); b.ConfigureByConvention(); b.Property(x => x.Term).HasMaxLength(80).IsRequired();
            b.Property(x => x.MasteryScore).HasPrecision(5, 2);
            b.HasIndex(x => new { x.ChildProfileId, x.Term }).IsUnique();
            b.HasOne<ChildProfile>().WithMany().HasForeignKey(x => x.ChildProfileId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
