using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TasteZambia.API.Data.Entities;

namespace TasteZambia.API.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> e)
    {
        e.ToTable("media_assets");
        e.HasIndex(x => x.UserId);
        e.HasIndex(x => x.FamilyRecipeId);
        e.HasIndex(x => x.ContributionId);
        e.Property(x => x.ContentType).HasMaxLength(80);
        e.Property(x => x.Caption).HasMaxLength(300);
    }
}

public class FamilyRecipeConfiguration : IEntityTypeConfiguration<FamilyRecipe>
{
    public void Configure(EntityTypeBuilder<FamilyRecipe> e)
    {
        e.ToTable("family_recipes");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.OwnerId, x.UpdatedAt });
        e.Property(x => x.LocalName).HasMaxLength(120).IsRequired();
        e.Property(x => x.Description).HasMaxLength(400);
        e.Property(x => x.Province).HasMaxLength(40);
        e.Property(x => x.Privacy).HasConversion<int>();
        e.Property(x => x.Transcript).HasConversion<int>();
        e.UseXminConcurrency();

        e.HasMany(x => x.Members).WithOne().HasForeignKey(m => m.FamilyRecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Notes).WithOne().HasForeignKey(n => n.FamilyRecipeId).OnDelete(DeleteBehavior.Cascade);
        e.HasMany(x => x.Media).WithOne().HasForeignKey(m => m.FamilyRecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class FamilyMemberConfiguration : IEntityTypeConfiguration<FamilyMember>
{
    public void Configure(EntityTypeBuilder<FamilyMember> e)
    {
        e.ToTable("family_members");
        e.HasKey(x => x.Id);
        e.HasIndex(x => new { x.FamilyRecipeId, x.UserId });
        e.Property(x => x.DisplayName).HasMaxLength(120).IsRequired();
        e.Property(x => x.State).HasConversion<int>();
    }
}

public class FamilyNoteConfiguration : IEntityTypeConfiguration<FamilyNote>
{
    public void Configure(EntityTypeBuilder<FamilyNote> e)
    {
        e.ToTable("family_notes");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.FamilyRecipeId);
        e.Property(x => x.Body).HasMaxLength(2000).IsRequired();
    }
}

public class FamilyInviteConfiguration : IEntityTypeConfiguration<FamilyInvite>
{
    public void Configure(EntityTypeBuilder<FamilyInvite> e)
    {
        e.ToTable("family_invites");
        e.HasKey(x => x.Id);
        e.HasIndex(x => x.Code).IsUnique();
        e.Property(x => x.Code).HasMaxLength(8).IsRequired();

        // Invites cascade with the recipe; the member row they redeem into does not
        // cascade from here, so no FK is declared against FamilyMember.
        e.HasOne<FamilyRecipe>().WithMany().HasForeignKey(x => x.FamilyRecipeId).OnDelete(DeleteBehavior.Cascade);
    }
}
