namespace HeadendHQ.Core.Catalog;

public interface IExternalRef
{
    string? SourceKey { get; }
    string? ExternalId { get; }
}

public static class ExternalRefExtensions
{
    public static string? ExternalIdFor(this IExternalRef entity, string sourceKey) =>
        entity.SourceKey == sourceKey ? entity.ExternalId : null;
}
