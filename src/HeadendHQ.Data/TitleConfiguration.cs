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

        // Descriptive fields, artwork ids and colours are plain columns. The two string lists and
        // the cast go to JSON columns — nothing queries them, the NFO writer just reads them back.
        builder.PrimitiveCollection(e => e.Genres);
        builder.PrimitiveCollection(e => e.Sets);
        builder.OwnsMany(e => e.Cast, c => c.ToJson());

        builder.HasIndex(e => e.StartUtc);
        builder.HasIndex(e => e.SourceId);
    }
}
