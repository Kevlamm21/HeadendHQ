using HeadendHQ.Core.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class StreamingServiceAssetConfiguration : IEntityTypeConfiguration<StreamingServiceAsset>
{
    public void Configure(EntityTypeBuilder<StreamingServiceAsset> builder)
    {
        builder.ToTable("StreamingServiceAssets");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Service).IsUnique();
    }
}
