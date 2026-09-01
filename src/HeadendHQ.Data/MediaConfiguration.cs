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

        // Content-addressed: identical bytes are stored once, and a re-fetch that hashes to the
        // same value is recognised as unchanged without touching any asset pointing at it.
        builder.HasIndex(e => e.Sha256).IsUnique();
    }
}
