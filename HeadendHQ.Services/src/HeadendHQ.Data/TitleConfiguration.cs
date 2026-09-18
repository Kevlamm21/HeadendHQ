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

        builder.PrimitiveCollection(e => e.Genres);
        builder.PrimitiveCollection(e => e.Sets);
        builder.OwnsMany(e => e.Cast, c => c.ToJson());

        builder.HasIndex(e => e.StartUtc);
        builder.HasIndex(e => e.SourceId);
    }
}
