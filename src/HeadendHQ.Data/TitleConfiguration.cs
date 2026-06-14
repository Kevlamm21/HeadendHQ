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
    }

}
