using HeadendHQ.Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class SourceSettingsConfiguration : IEntityTypeConfiguration<SourceSettings>
{
    public void Configure(EntityTypeBuilder<SourceSettings> builder)
    {
        builder.ToTable("SourceSettings");
        builder.HasKey(e => e.Id);
        builder.PrimitiveCollection(e => e.DiscoverySportSlugs);
    }
}
