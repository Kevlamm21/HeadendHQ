using HeadendHQ.HdHomerun;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class HdHomerunSettingsConfiguration : IEntityTypeConfiguration<HdHomerunSettings>
{
    public void Configure(EntityTypeBuilder<HdHomerunSettings> builder)
    {
        builder.ToTable("HdHomerunSettings");
        builder.HasKey(e => e.Id);
    }
}
