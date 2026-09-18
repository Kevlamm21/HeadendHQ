using HeadendHQ.Core.Streaming;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class StreamingServiceConfiguration : IEntityTypeConfiguration<StreamingService>
{
    public void Configure(EntityTypeBuilder<StreamingService> builder)
    {
        builder.ToTable("StreamingServices");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Key).IsUnique();

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("StreamingServiceLogos");
            logo.WithOwner().HasForeignKey(l => l.StreamingServiceId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.StreamingServiceId, l.Variant }).IsUnique();
        });
    }
}
