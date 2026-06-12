using System.Text.Json;
using HeadendHQ.Core.Titles;
using HeadendHQ.DummyVideo;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
                v => JsonSerializer.Deserialize<Dictionary<TitleType, string>>(v, JsonSerializerOptions.Default)!,
                new ValueComparer<Dictionary<TitleType, string>>(
                    (a, b) => JsonSerializer.Serialize(a, JsonSerializerOptions.Default) == JsonSerializer.Serialize(b, JsonSerializerOptions.Default),
                    v => JsonSerializer.Serialize(v, JsonSerializerOptions.Default).GetHashCode(),
                    v => JsonSerializer.Deserialize<Dictionary<TitleType, string>>(JsonSerializer.Serialize(v, JsonSerializerOptions.Default), JsonSerializerOptions.Default)!));
    }
}
