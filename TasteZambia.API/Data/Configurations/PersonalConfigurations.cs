using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> e)
    {
        e.ToTable("user_profiles");
        e.HasKey(x => x.UserId);
        e.Property(x => x.DisplayName).HasMaxLength(120);
        e.Property(x => x.Location).HasMaxLength(120);
        e.Property(x => x.Languages).HasMaxLength(200);
        e.HasOne<ArchiveUser>().WithOne().HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class OnboardingChoicesConfiguration : IEntityTypeConfiguration<OnboardingChoices>
{
    public void Configure(EntityTypeBuilder<OnboardingChoices> e)
    {
        e.ToTable("onboarding_choices");
        e.HasKey(x => x.UserId);
        e.Property(x => x.Language).HasMaxLength(40);
        e.Property(x => x.Who).HasMaxLength(80);
        e.Property(x => x.Tastes).HasMaxLength(200);
        e.HasOne<ArchiveUser>().WithOne().HasForeignKey<OnboardingChoices>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SavedDishConfiguration : IEntityTypeConfiguration<SavedDish>
{
    public void Configure(EntityTypeBuilder<SavedDish> e)
    {
        e.ToTable("saved_dishes");
        e.HasKey(x => new { x.UserId, x.DishId });
        e.Property(x => x.DishId).HasMaxLength(64);
        e.HasOne<ArchiveUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class CookProgressConfiguration : IEntityTypeConfiguration<CookProgress>
{
    public void Configure(EntityTypeBuilder<CookProgress> e)
    {
        e.ToTable("cook_progress");
        e.HasKey(x => new { x.UserId, x.DishId, x.StepNumber });
        e.Property(x => x.DishId).HasMaxLength(64);
        e.HasOne<ArchiveUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
