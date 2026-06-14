using HeadendHQ.Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class GlobalSettingsConfiguration : IEntityTypeConfiguration<GlobalSettings>
{
    public void Configure(EntityTypeBuilder<GlobalSettings> builder)
    {
        builder.ToTable("GlobalSettings");
        builder.HasKey(e => e.Id);
        builder.PrimitiveCollection(e => e.EnabledStreamingServices);
    }
}
