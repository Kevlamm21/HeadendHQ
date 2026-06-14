using System.Text;
using System.Xml;
using System.Xml.Linq;
using HeadendHQ.Core;
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
        var doc = BuildDocument(title);

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

    private static XDocument BuildDocument(Title title)
    {
        var meta = title.Metadata;
        var name = title.Name;

        var movie = new XElement("movie",
            new XElement("title", name),
            new XElement("originaltitle", name));

        if (meta?.Plot is not null) movie.Add(new XElement("plot", meta.Plot));
        if (meta?.Tagline is not null) movie.Add(new XElement("tagline", meta.Tagline));
        if (meta?.ContentRating is not null) movie.Add(new XElement("mpaa", meta.ContentRating));
        if (title.StartUtc is not null) movie.Add(new XElement("premiered", title.StartUtc.Value.ToString("yyyy-MM-dd")));
        if (meta?.Studio is not null) movie.Add(new XElement("studio", meta.Studio));

        foreach (var genre in meta?.Genres ?? [])
            movie.Add(new XElement("genre", genre));

        foreach (var set in meta?.Sets ?? [])
            movie.Add(new XElement("set", new XElement("name", set)));

        if (title.IsLive)
            movie.Add(new XElement("set", new XElement("name", "Live")));

        if (meta?.UniqueId is not null)
            movie.Add(new XElement("uniqueid", new XAttribute("type", "external"), meta.UniqueId));

        movie.Add(new XElement("thumb", new XAttribute("aspect", "poster"), $"{name}.jpg"));
        movie.Add(new XElement("fanart",
            new XElement("thumb", $"{name}-fanart-1.jpg")));

        return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), movie);
    }
}
