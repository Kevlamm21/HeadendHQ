using HeadendHQ.Core.Shared;

namespace HeadendHQ.Core.Titles.Specifications;

public class TitlesNeedingArtworkSpec : ISpecification<Title>
{
    public IQueryable<Title> Apply(IQueryable<Title> q) =>
        q.Where(t => t.Type == TitleType.SportingEvent && t.DummyVideoPath != null && t.PosterPath == null);
}
