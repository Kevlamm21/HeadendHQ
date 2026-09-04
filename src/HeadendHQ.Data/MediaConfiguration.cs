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

        // Checked before downloading, so an image we already hold costs no request at all.
        builder.HasIndex(e => e.SourceUrl).IsUnique().HasFilter("\"SourceUrl\" IS NOT NULL");

        // The season-start sweep: every headshot, optionally narrowed to one league.
        builder.HasIndex(e => new { e.Purpose, e.LeagueId });
    }
}
