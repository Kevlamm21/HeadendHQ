using HeadendHQ.Core.Iptv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class IptvProgrammeConfiguration : IEntityTypeConfiguration<IptvProgramme>
{
    public void Configure(EntityTypeBuilder<IptvProgramme> builder)
    {
        builder.ToTable("IptvProgrammes");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.GuideNumber, e.StartUtc });
    }
}
