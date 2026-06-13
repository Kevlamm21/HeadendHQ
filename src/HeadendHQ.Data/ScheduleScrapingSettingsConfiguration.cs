using HeadendHQ.ScheduleScraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class ScheduleScrapingSettingsConfiguration : IEntityTypeConfiguration<ScheduleScrapingSettings>
{
    public void Configure(EntityTypeBuilder<ScheduleScrapingSettings> builder)
    {
        builder.ToTable("ScheduleScrapingSettings");
        builder.HasKey(e => e.Id);
    }
}
