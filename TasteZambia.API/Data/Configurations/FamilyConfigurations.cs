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
