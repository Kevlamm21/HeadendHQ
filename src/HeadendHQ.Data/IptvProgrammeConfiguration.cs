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

        // The only read is "what is on this channel around then", so the pair is the index.
        builder.HasIndex(e => new { e.GuideNumber, e.StartUtc });
    }
}
