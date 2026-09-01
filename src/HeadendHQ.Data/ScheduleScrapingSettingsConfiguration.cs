using HeadendHQ.Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class ScheduleScrapingSettingsConfiguration : IEntityTypeConfiguration<ScheduleScrapingSettings>
{
    public void Configure(EntityTypeBuilder<ScheduleScrapingSettings> builder)
    {
        builder.ToTable("ScheduleScrapingSettings");
        builder.HasKey(e => e.Id);

        // Existing rows must land on the real default, not 0 (which would mean no players at all).
        builder.Property(e => e.MaxAthletesPerTeam).HasDefaultValue(12);
    }
}
