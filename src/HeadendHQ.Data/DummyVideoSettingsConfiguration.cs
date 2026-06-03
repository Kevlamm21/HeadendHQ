using System.Text.Json;
using HeadendHQ.Core.Titles;
using HeadendHQ.DummyVideo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HeadendHQ.Data;

internal class DummyVideoSettingsConfiguration : IEntityTypeConfiguration<DummyVideoSettings>
{
    public void Configure(EntityTypeBuilder<DummyVideoSettings> builder)
    {
        builder.ToTable("DummyVideoSettings");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.LibraryPaths)
            .HasConversion(
                v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default),
                v => JsonSerializer.Deserialize<Dictionary<TitleType, string>>(v, JsonSerializerOptions.Default)!);
    }
}
