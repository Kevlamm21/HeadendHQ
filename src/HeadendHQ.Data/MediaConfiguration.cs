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

        // Checked before downloading, so an image we already hold costs no request at all. Purpose
        // is part of the key because it decides normalization: the same URL fetched as a headshot and
        // as a team logo produces different pixels, so both may be held at once.
        builder.HasIndex(e => new { e.SourceUrl, e.Purpose })
            .IsUnique()
            .HasFilter("\"SourceUrl\" IS NOT NULL");

        // The season-start sweep: every headshot, optionally narrowed to one league.
        builder.HasIndex(e => new { e.Purpose, e.LeagueId });
    }
}
