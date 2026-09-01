namespace HeadendHQ.Core.Titles;

/// <summary>
/// One billed name on a title, denormalized to exactly what an NFO <c>&lt;actor&gt;</c> needs.
/// <para>
/// The name and role are copied rather than looked up so the NFO writer does no joins and a title
/// keeps reading correctly even if the underlying athlete is later removed from the catalog.
/// </para>
/// </summary>
public class TitleCastMember
{
    private TitleCastMember() { }

    internal TitleCastMember(string name, string? role, int? headshotImageId, int order)
    {
        Name = name;
        Role = role;
        HeadshotImageId = headshotImageId;
        Order = order;
    }

    public int Id { get; init; }
    public Guid TitleId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Role { get; private set; }
    public int? HeadshotImageId { get; private set; }
    public int Order { get; private set; }
}
