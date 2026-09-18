using HeadendHQ.Core.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class SportingEventConfiguration : IEntityTypeConfiguration<SportingEvent>
{
    public void Configure(EntityTypeBuilder<SportingEvent> builder)
    {
        builder.ToTable("SportingEvents");
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.SourceKey, e.ExternalId }).IsUnique();

        builder.HasIndex(e => e.StartUtc);
        builder.HasIndex(e => e.TitleId);

        builder.OwnsMany(e => e.Cast, cast =>
        {
            cast.ToTable("SportingEventCast");
            cast.WithOwner().HasForeignKey(c => c.SportingEventId);
            cast.HasKey(c => c.Id);
            cast.HasIndex(c => new { c.SportingEventId, c.Order });
        });
    }
}
