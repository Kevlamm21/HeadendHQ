using HeadendHQ.Core.Assets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class TeamAssetConfiguration : IEntityTypeConfiguration<TeamAsset>
{
    public void Configure(EntityTypeBuilder<TeamAsset> builder)
    {
        builder.ToTable("TeamAssets");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.TeamName, e.League }).IsUnique();
    }
}
