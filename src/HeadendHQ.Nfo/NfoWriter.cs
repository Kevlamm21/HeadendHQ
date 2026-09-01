using System.Text;
using System.Xml;
using System.Xml.Linq;
using HeadendHQ.Core;
using HeadendHQ.Core.Settings;
using HeadendHQ.Core.Shared;
using HeadendHQ.Core.Titles;
using Microsoft.Extensions.Logging;

namespace HeadendHQ.Nfo;

public class NfoWriter(IReadModel readModel, ILogger<NfoWriter> logger) : INfoWriter
{
    public async Task WriteAsync(Title title, CancellationToken ct = default)
    {
        if (title.VodLauncherPath is null)
        {
            logger.LogWarning("Cannot write NFO for title {Id} ({Name}): VodLauncherPath is not set.", title.Id, title.Name);
            return;
        }

        var nfoPath = Path.Combine(title.VodLauncherPath, $"{title.Name}.nfo");

        // The cast is already on the title, in billing order, with names and roles resolved. Whatever
        // produced the title did that mapping once; nothing here needs to know what an athlete is.
        var cast = title.Cast.OrderBy(c => c.Order).ToList();

        var globalSettings = await readModel.SingleOrDefault(new GlobalSettingsSpec(), ct);
        var thumbMode = globalSettings?.ActorThumbMode ?? ActorThumbMode.LocalFile;

        if (cast.Count > 0 && thumbMode is ActorThumbMode.Url && string.IsNullOrEmpty(globalSettings?.PublicBaseUrl))
            logger.LogWarning(
                "PublicBaseUrl is not set; omitting {Count} headshot thumb(s) from the NFO for title {Id} ({Name}).",
                cast.Count, title.Id, title.Name);

        var doc = BuildDocument(title, cast, thumbMode, globalSettings?.PublicBaseUrl);

        await using var stream = new FileStream(nfoPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        var settings = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), Async = true };
        await using var writer = XmlWriter.Create(stream, settings);
        await doc.SaveAsync(writer, ct);
        await writer.FlushAsync();

        logger.LogInformation("Wrote NFO for title {Id} ({Name}) to {Path}.", title.Id, title.Name, nfoPath);
    }

    public async Task WriteForTitleAsync(Guid titleId, CancellationToken ct = default)
    {
        var title = await readModel.SingleOrDefault(new EntityByIdSpecification<Title, Guid>(titleId), ct);
        if (title is null)
        {
            logger.LogWarning("Cannot write NFO: title {Id} not found.", titleId);
            return;
        }

        await WriteAsync(title, ct);
    }

    /// <summary>
    /// Written for Jellyfin specifically. Its NFO parser reads a fixed set of tags and ignores the
    /// rest, and its image handling keys off the <c>aspect</c> attribute, so the shape here is not
    /// interchangeable with Kodi's or Plex's.
    /// </summary>
    private static XDocument BuildDocument(
        Title title, List<TitleCastMember> cast, ActorThumbMode thumbMode, string? publicBaseUrl)
    {
        var meta = title.Metadata;
        var name = title.Name;

        var movie = new XElement("movie",
            new XElement("title", name),
            new XElement("originaltitle", name));

        // These are synthetic launcher entries, not films. Without this Jellyfin's online providers
        // try to match them against real movies and overwrite everything below.
        movie.Add(new XElement("lockdata", "true"));

        if (meta?.Plot is not null) movie.Add(new XElement("plot", meta.Plot));

        // Jellyfin has no venue tag, so the venue rides along on the tagline rather than being
        // written to an element that would simply be discarded.
        if (Tagline(meta) is { } tagline) movie.Add(new XElement("tagline", tagline));

        if (meta?.ContentRating is not null) movie.Add(new XElement("mpaa", meta.ContentRating));
        if (title.StartUtc is not null) movie.Add(new XElement("premiered", title.StartUtc.Value.ToString("yyyy-MM-dd")));
        if (meta?.Studio is not null) movie.Add(new XElement("studio", meta.Studio));

        foreach (var genre in meta?.Genres ?? [])
            movie.Add(new XElement("genre", genre));

        // A movie has exactly one collection: Jellyfin's parser assigns CollectionName, so a second
        // <set> silently replaces the first. Anything else that wants to be a label is a tag.
        if ((meta?.Sets ?? []).FirstOrDefault() is { } collection)
            movie.Add(new XElement("set", new XElement("name", collection)));

        foreach (var extra in (meta?.Sets ?? []).Skip(1))
            movie.Add(new XElement("tag", extra));

        if (title.IsLive)
            movie.Add(new XElement("tag", "Live"));

        if (meta?.UniqueId is not null)
            movie.Add(new XElement("uniqueid",
                new XAttribute("type", "espn"), new XAttribute("default", "true"), meta.UniqueId));

        // aspect drives which image slot Jellyfin fills: default/poster -> Primary,
        // landscape -> Thumb, clearlogo -> Logo, and a <thumb> nested in <fanart> -> Backdrop.
        // Naming both landscape renders after a backdrop is what previously left Thumb empty.
        movie.Add(new XElement("thumb", new XAttribute("aspect", "poster"), TitleArtworkFiles.Poster(name)));
        movie.Add(new XElement("thumb", new XAttribute("aspect", "landscape"), TitleArtworkFiles.Thumb(name)));

        if (title.Artwork.WordmarkImageId is not null)
            movie.Add(new XElement("thumb", new XAttribute("aspect", "clearlogo"), TitleArtworkFiles.ClearLogo(name)));

        movie.Add(new XElement("fanart", new XElement("thumb", TitleArtworkFiles.Backdrop(name))));

        for (var order = 0; order < cast.Count; order++)
        {
            var member = cast[order];
            var actor = new XElement("actor", new XElement("name", member.Name));

            if (!string.IsNullOrWhiteSpace(member.Role))
                actor.Add(new XElement("role", member.Role));

            actor.Add(new XElement("order", order));

            if (ActorThumb(member, thumbMode, publicBaseUrl) is { } thumb)
                actor.Add(new XElement("thumb", thumb));

            movie.Add(actor);
        }

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), movie);
    }

    private static string? ActorThumb(TitleCastMember member, ActorThumbMode mode, string? publicBaseUrl)
    {
        if (member.HeadshotImageId is not { } imageId)
            return null;

        return mode is ActorThumbMode.LocalFile
            ? TitleArtworkFiles.ActorThumb(member)
            : string.IsNullOrEmpty(publicBaseUrl) ? null : $"{publicBaseUrl}/media/images/{imageId}";
    }

    private static string? Tagline(TitleMetadata? meta)
    {
        if (meta is null)
            return null;

        return (meta.Tagline, meta.VenueName) switch
        {
            (null or "", null or "") => null,
            (null or "", var venue) => venue,
            (var line, null or "") => line,
            var (line, venue) => $"{line} \u2014 {venue}",
        };
    }
}
