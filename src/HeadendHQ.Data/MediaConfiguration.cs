using HeadendHQ.Core.Media;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class ImageConfiguration : IEntityTypeConfiguration<Image>
{
    public void Configure(EntityTypeBuilder<Image> builder)
    {
        builder.ToTable("Images");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => e.Sha256).IsUnique();

        builder.HasIndex(e => new { e.SourceUrl, e.Purpose })
            .IsUnique()
            .HasFilter("\"SourceUrl\" IS NOT NULL");

        builder.HasIndex(e => new { e.Purpose, e.LeagueId });
    }
}
