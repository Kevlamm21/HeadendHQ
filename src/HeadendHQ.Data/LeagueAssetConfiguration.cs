using HeadendHQ.Core.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class LeagueAssetConfiguration : IEntityTypeConfiguration<LeagueAsset>
{
    public void Configure(EntityTypeBuilder<LeagueAsset> builder)
    {
        builder.ToTable("LeagueAssets");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.League, e.Variant }).IsUnique();
    }
}
