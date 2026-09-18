using HeadendHQ.Core.Iptv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class IptvChannelConfiguration : IEntityTypeConfiguration<IptvChannel>
{
    public void Configure(EntityTypeBuilder<IptvChannel> builder)
    {
        builder.ToTable("IptvChannels");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.GuideNumber).IsUnique();
        builder.HasIndex(e => e.CallSign);
    }
}
