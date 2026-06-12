using HeadendHQ.Core.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class WordMarkConfiguration : IEntityTypeConfiguration<WordMark>
{
    public void Configure(EntityTypeBuilder<WordMark> builder)
    {
        builder.ToTable("WordMarks");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.League, e.Variant }).IsUnique();
    }
}
