using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Titles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class ScheduleScraperSettingsConfiguration : IEntityTypeConfiguration<ScheduleScraperSettings>
{
    public void Configure(EntityTypeBuilder<ScheduleScraperSettings> builder)
    {
        builder.ToTable("ScheduleScraperSettings");
        builder.HasKey(e => e.Id);
        builder.PrimitiveCollection(e => e.EnabledStreamingServices);
    }
}
