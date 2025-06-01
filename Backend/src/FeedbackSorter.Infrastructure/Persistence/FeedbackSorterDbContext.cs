using FeedbackSorter.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace FeedbackSorter.Infrastructure.Persistence;

public class FeedbackSorterDbContext : DbContext
{
    public DbSet<UserFeedbackDb> UserFeedbacks { get; set; } = null!;
    public DbSet<FeatureCategoryDb> FeatureCategories { get; set; } = null!;
    public DbSet<UserFeedbackSelectedCategoryDb> UserFeedbackSelectedCategories { get; set; } = null!; // DbSet for the new join entity

    public FeedbackSorterDbContext(DbContextOptions<FeedbackSorterDbContext> options)
        : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // UserFeedbackDb Configuration
        modelBuilder.Entity<UserFeedbackDb>(userFeedbackEntity => // 'entity' is EntityTypeBuilder<UserFeedbackDb>
        {
            userFeedbackEntity.HasKey(e => e.Id);
            userFeedbackEntity.Property(e => e.Text).HasMaxLength(2000).IsRequired();
            userFeedbackEntity.Property(e => e.AnalysisStatus).IsRequired();

            // Configure flattened AnalysisResult properties
            userFeedbackEntity.Property(e => e.AnalysisResultTitle).HasMaxLength(50);
            userFeedbackEntity.Property(e => e.AnalysisResultSentiment);
            userFeedbackEntity.Property(e => e.AnalysisResultAnalyzedAt);

            // Configure flattened LastFailureDetails properties
            userFeedbackEntity.Property(e => e.LastFailureDetailsReason);
            userFeedbackEntity.Property(e => e.LastFailureDetailsMessage);
            userFeedbackEntity.Property(e => e.LastFailureDetailsOccurredAt);
            userFeedbackEntity.Property(e => e.LastFailureDetailsAttemptNumber);

            // Configure the many-to-many relationship for AnalysisResultFeatureCategories
            userFeedbackEntity.HasMany(e => e.AnalysisResultFeatureCategories)
                .WithMany()
                .UsingEntity(j => j.ToTable("UserFeedbackAnalysisResultFeatureCategories"));

            userFeedbackEntity.HasMany(e => e.SelectedFeedbackCategories)
                  .WithOne(sfc => sfc.UserFeedback)
                  .HasForeignKey(sfc => sfc.UserFeedbackDbId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserFeedbackSelectedCategoryDb>(selectedCategoryEntity =>
        {
            selectedCategoryEntity.HasKey(sfc => new { sfc.UserFeedbackDbId, sfc.FeedbackCategoryValue });
            selectedCategoryEntity.Property(sfc => sfc.FeedbackCategoryValue).HasMaxLength(50);
        });

        modelBuilder.Entity<FeatureCategoryDb>(featureCategoryEntity =>
        {
            featureCategoryEntity.HasKey(e => e.Id);
            featureCategoryEntity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            featureCategoryEntity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
