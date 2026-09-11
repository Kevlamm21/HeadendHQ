using HeadendHQ.Core.Catalog.Broadcasters;
using HeadendHQ.Core.Catalog.Leagues;
using HeadendHQ.Core.Catalog.Sports;
using HeadendHQ.Core.Catalog.Teams;
using HeadendHQ.Core.Catalog;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;

namespace HeadendHQ.Data;

internal class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("Sports");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => new { e.SourceKey, e.ExternalId });
    }
}

internal class LeagueConfiguration : IEntityTypeConfiguration<League>
{
    public void Configure(EntityTypeBuilder<League> builder)
    {
        builder.ToTable("Leagues");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.HasIndex(e => e.SportId);

        builder.HasIndex(e => new { e.SourceKey, e.ExternalId });

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("LeagueLogos");
            logo.WithOwner().HasForeignKey(l => l.LeagueId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.LeagueId, l.Variant, l.Label }).IsUnique();
        });

        builder.OwnsMany(e => e.Wordmarks, mark =>
        {
            mark.ToTable("LeagueWordmarks");
            mark.WithOwner().HasForeignKey(w => w.LeagueId);
            mark.HasKey(w => w.Id);
            mark.HasIndex(w => new { w.LeagueId, w.Variant }).IsUnique();

            mark.Ignore(w => w.Label);
            mark.Ignore(w => w.Origin);
            mark.Ignore(w => w.IsSelected);
        });
    }
}

internal class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => new { e.LeagueId, e.DisplayName }).IsUnique();

        builder.HasIndex(e => new { e.SourceKey, e.ExternalId });

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("TeamLogos");
            logo.WithOwner().HasForeignKey(l => l.TeamId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.TeamId, l.Variant, l.Label }).IsUnique();
        });
    }
}

internal class BroadcasterConfiguration : IEntityTypeConfiguration<Broadcaster>
{
    public void Configure(EntityTypeBuilder<Broadcaster> builder)
    {
        builder.ToTable("Broadcasters");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.PrimitiveCollection(e => e.Aliases);

        builder.HasIndex(e => new { e.SourceKey, e.ExternalId });

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("BroadcasterLogos");
            logo.WithOwner().HasForeignKey(l => l.BroadcasterId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.BroadcasterId, l.Variant, l.Label }).IsUnique();
        });
    }
}
