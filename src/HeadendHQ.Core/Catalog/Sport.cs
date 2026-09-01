using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Catalog;

public class Sport : IEntity<int>
{
    private Sport() { }

    public Sport(string slug, string name)
    {
        Slug = slug;
        Name = name;
    }

    public int Id { get; init; }
    public string Slug { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    public List<ExternalRef> ExternalRefs { get; private set; } = [];

    public void Rename(string name) => Name = name;

    public void TrackSource(string sourceKey, string externalId) =>
        ExternalRefs.Track(sourceKey, externalId);
}
