using HeadendHQ.Core.HdHomerun;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class XmltvCacheConfiguration : IEntityTypeConfiguration<XmltvCache>
{
    public void Configure(EntityTypeBuilder<XmltvCache> builder)
    {
        builder.ToTable("XmltvCache");
        builder.HasKey(e => e.Id);
    }
}
