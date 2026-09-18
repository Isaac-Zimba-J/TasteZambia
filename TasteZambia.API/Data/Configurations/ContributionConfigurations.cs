using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

public class ContributionConfiguration : IEntityTypeConfiguration<Contribution>
{
    public void Configure(EntityTypeBuilder<Contribution> e)
    {
        e.ToTable("contributions");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.UserId, x.SubmittedAt });
        e.HasIndex(x => x.Status);
        e.Property(x => x.Status).HasConversion<int>();
        e.Property(x => x.LocalName).HasMaxLength(120);
        e.Property(x => x.EnglishDescription).HasMaxLength(400);
        e.Property(x => x.Province).HasMaxLength(40);
        e.Property(x => x.MealType).HasMaxLength(40);
        e.Property(x => x.Language).HasMaxLength(40);
        e.Property(x => x.ContributorName).HasMaxLength(120);
        e.Property(x => x.ContributorLocation).HasMaxLength(120);
        e.Property(x => x.PublishedDishId).HasMaxLength(80);
        e.HasOne<ArchiveUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Ingredients).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Steps).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Events).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Flags).WithOne().HasForeignKey(x => x.ContributionId).OnDelete(DeleteBehavior.Cascade);
        e.UseXminConcurrency();
    }
}

public class ContributionIngredientConfiguration : IEntityTypeConfiguration<ContributionIngredient>
{
    public void Configure(EntityTypeBuilder<ContributionIngredient> e)
    {
        e.ToTable("contribution_ingredients");
        e.Property(x => x.IngredientKey).HasMaxLength(64);
        e.Property(x => x.DisplayName).HasMaxLength(120);
        e.Property(x => x.DisplaySubtitle).HasMaxLength(120);
        e.Property(x => x.Quantity).HasMaxLength(80);
    }
}

public class ContributionStepConfiguration : IEntityTypeConfiguration<ContributionStep>
{
    public void Configure(EntityTypeBuilder<ContributionStep> e) => e.ToTable("contribution_steps");
}

public class ReviewEventConfiguration : IEntityTypeConfiguration<ReviewEvent>
{
    public void Configure(EntityTypeBuilder<ReviewEvent> e)
    {
        e.ToTable("review_events");
        e.Property(x => x.Kind).HasConversion<int>();
        e.Property(x => x.Actor).HasMaxLength(120);
        e.HasIndex(x => new { x.ContributionId, x.At });
    }
}

public class FlaggedFieldConfiguration : IEntityTypeConfiguration<FlaggedField>
{
    public void Configure(EntityTypeBuilder<FlaggedField> e)
    {
        e.ToTable("flagged_fields");
        e.Property(x => x.Field).HasMaxLength(80);
    }
}
