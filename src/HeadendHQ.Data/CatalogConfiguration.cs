using HeadendHQ.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

/// <summary>
/// Catalog aggregates own their external refs and logo slots, so they load with their parent and
/// no specification ever has to reach for an Include. That is what keeps EF Core out of
/// <c>HeadendHQ.Core</c>.
/// </summary>
internal class SportConfiguration : IEntityTypeConfiguration<Sport>
{
    public void Configure(EntityTypeBuilder<Sport> builder)
    {
        builder.ToTable("Sports");
        builder.HasKey(e => e.Id);
        builder.HasIndex(e => e.Slug).IsUnique();
        builder.OwnsMany(e => e.ExternalRefs, ExternalRefs.Map<Sport>("SportExternalRefs", "SportId"));
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

        builder.OwnsMany(e => e.ExternalRefs, ExternalRefs.Map<League>("LeagueExternalRefs", "LeagueId"));

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("LeagueLogos");
            logo.WithOwner().HasForeignKey(l => l.LeagueId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.LeagueId, l.Variant, l.Rel }).IsUnique();
            logo.OwnsOne(l => l.Image);
            logo.Navigation(l => l.Image).IsRequired();
        });

        builder.OwnsMany(e => e.Wordmarks, mark =>
        {
            mark.ToTable("LeagueWordmarks");
            mark.WithOwner().HasForeignKey(w => w.LeagueId);
            mark.HasKey(w => w.Id);
            mark.HasIndex(w => new { w.LeagueId, w.Variant }).IsUnique();
            mark.OwnsOne(w => w.Image);
            mark.Navigation(w => w.Image).IsRequired();
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

        builder.OwnsMany(e => e.ExternalRefs, ExternalRefs.Map<Team>("TeamExternalRefs", "TeamId"));

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("TeamLogos");
            logo.WithOwner().HasForeignKey(l => l.TeamId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.TeamId, l.Rel }).IsUnique();
            logo.OwnsOne(l => l.Image);
            logo.Navigation(l => l.Image).IsRequired();
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

        builder.OwnsMany(e => e.ExternalRefs, ExternalRefs.Map<Broadcaster>("BroadcasterExternalRefs", "BroadcasterId"));

        builder.OwnsMany(e => e.Logos, logo =>
        {
            logo.ToTable("BroadcasterLogos");
            logo.WithOwner().HasForeignKey(l => l.BroadcasterId);
            logo.HasKey(l => l.Id);
            logo.HasIndex(l => new { l.BroadcasterId, l.Variant }).IsUnique();
            logo.OwnsOne(l => l.Image);
            logo.Navigation(l => l.Image).IsRequired();
        });
    }
}

internal class CatalogSyncStateConfiguration : IEntityTypeConfiguration<CatalogSyncState>
{
    public void Configure(EntityTypeBuilder<CatalogSyncState> builder)
    {
        builder.ToTable("CatalogSyncState");
        builder.HasKey(e => e.Id);
        builder.PrimitiveCollection(e => e.CompletedLeagueSlugs);
    }
}

internal static class ExternalRefs
{
    /// <summary>
    /// Every aggregate maps its external refs the same way; only the table and owner column differ.
    /// The unique index is what makes "find the team ESPN calls 12" a single indexed lookup.
    /// </summary>
    public static Action<OwnedNavigationBuilder<TOwner, ExternalRef>> Map<TOwner>(string table, string ownerColumn)
        where TOwner : class => builder =>
    {
        builder.ToTable(table);
        builder.WithOwner().HasForeignKey(ownerColumn);
        builder.Property<int>("Id");
        builder.HasKey("Id");
        builder.HasIndex(ownerColumn, nameof(ExternalRef.SourceKey)).IsUnique();
        builder.HasIndex(nameof(ExternalRef.SourceKey), nameof(ExternalRef.ExternalId));
    };
}
