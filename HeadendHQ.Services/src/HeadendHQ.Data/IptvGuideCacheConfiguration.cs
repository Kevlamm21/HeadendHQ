using HeadendHQ.Core.Iptv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class IptvGuideCacheConfiguration : IEntityTypeConfiguration<IptvGuideCache>
{
    public void Configure(EntityTypeBuilder<IptvGuideCache> builder)
    {
        builder.ToTable("IptvGuideCache");
        builder.HasKey(e => e.Id);
    }
}
