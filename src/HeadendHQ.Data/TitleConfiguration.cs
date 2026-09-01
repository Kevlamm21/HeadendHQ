using HeadendHQ.Core.Titles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class TitleConfiguration : IEntityTypeConfiguration<Title>
{
    public void Configure(EntityTypeBuilder<Title> builder)
    {
        builder.ToTable("Titles");
        builder.HasKey(e => e.Id);
        builder.OwnsOne(e => e.Metadata, b => b.ToJson());

        // Artwork is flat columns rather than JSON: these are image ids the composer reads by key.
        builder.OwnsOne(e => e.Artwork);
        builder.Navigation(e => e.Artwork).IsRequired();

        builder.OwnsMany(e => e.Cast, cast =>
        {
            cast.ToTable("TitleCast");
            cast.WithOwner().HasForeignKey(c => c.TitleId);
            cast.HasKey(c => c.Id);
            cast.HasIndex(c => new { c.TitleId, c.Order });
        });

        builder.HasIndex(e => e.StartUtc);
    }

}
