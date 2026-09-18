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

        var globalSettings = await readModel.SingleOrDefault(new GlobalSettingsSpec(), ct);
        var publicBaseUrl = globalSettings?.PublicBaseUrl;

        var doc = BuildDocument(title, publicBaseUrl);

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

    private XDocument BuildDocument(Title title, string? publicBaseUrl)
    {
        var name = title.Name;

        var movie = new XElement("movie",
            new XElement("title", name),
            new XElement("originaltitle", name));

        movie.Add(new XElement("lockdata", "true"));

        if (title.Plot is not null) movie.Add(new XElement("plot", title.Plot));
        if (title.Tagline is not null) movie.Add(new XElement("tagline", title.Tagline));
        if (title.ContentRating is not null) movie.Add(new XElement("mpaa", title.ContentRating));
        if (title.StartUtc is not null) movie.Add(new XElement("premiered", title.StartUtc.Value.ToString("yyyy-MM-dd")));
        if (title.Studio is not null) movie.Add(new XElement("studio", title.Studio));

        foreach (var genre in title.Genres)
            movie.Add(new XElement("genre", genre));

        foreach (var set in title.Sets)
            movie.Add(new XElement("tag", set));

        if (title.IsLive)
            movie.Add(new XElement("tag", "Live"));

        if (title.UniqueId is not null)
            movie.Add(new XElement("uniqueid",
                new XAttribute("type", UniqueIdType(title.UniqueId)), new XAttribute("default", "true"), title.UniqueId));

        AddArtwork(movie, title, publicBaseUrl, name);
        AddCast(movie, title.Cast, publicBaseUrl);

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), movie);
    }

    private void AddArtwork(XElement movie, Title title, string? publicBaseUrl, string name)
    {
        if (string.IsNullOrEmpty(publicBaseUrl))
        {
            if (title.HasArtwork)
                logger.LogWarning(
                    "PublicBaseUrl is not set; omitting artwork from the NFO for title {Id} ({Name}).",
                    title.Id, name);
            return;
        }

        if (title.PosterImageId is { } posterId)
            movie.Add(new XElement("thumb", new XAttribute("aspect", "poster"), MediaUrl(publicBaseUrl, posterId)));

        if (title.ThumbnailImageId is { } thumbId)
            movie.Add(new XElement("thumb", new XAttribute("aspect", "landscape"), MediaUrl(publicBaseUrl, thumbId)));

        if (title.ClearLogoImageId is { } logoId)
            movie.Add(new XElement("thumb", new XAttribute("aspect", "clearlogo"), MediaUrl(publicBaseUrl, logoId)));

        if (title.BackgroundImageId is { } backgroundId)
            movie.Add(new XElement("fanart", new XElement("thumb", MediaUrl(publicBaseUrl, backgroundId))));
    }

    private void AddCast(XElement movie, IReadOnlyList<TitleCastEntry> cast, string? publicBaseUrl)
    {
        if (cast.Count > 0 && string.IsNullOrEmpty(publicBaseUrl))
            logger.LogWarning("PublicBaseUrl is not set; omitting {Count} headshot thumb(s) from the NFO.", cast.Count);

        for (var order = 0; order < cast.Count; order++)
        {
            var member = cast[order];
            var actor = new XElement("actor", new XElement("name", member.Name));

            if (!string.IsNullOrWhiteSpace(member.Role))
                actor.Add(new XElement("role", member.Role));

            actor.Add(new XElement("order", order));

            if (member.HeadshotImageId is { } imageId && !string.IsNullOrEmpty(publicBaseUrl))
                actor.Add(new XElement("thumb", MediaUrl(publicBaseUrl, imageId)));

            movie.Add(actor);
        }
    }

    private static string UniqueIdType(string uniqueId) =>
        uniqueId.IndexOf(':') is > 0 and var colon ? uniqueId[..colon] : "headendhq";

    private static string MediaUrl(string publicBaseUrl, int imageId) =>
        $"{publicBaseUrl}/media/images/{imageId}";
}
