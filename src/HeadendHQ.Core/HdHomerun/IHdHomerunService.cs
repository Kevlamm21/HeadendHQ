namespace HeadendHQ.Core.HdHomerun;

public interface IHdHomerunService
{
    Task RefreshXmltvAsync(CancellationToken cancellationToken = default);
    Task<string?> GetXmltvContentAsync(CancellationToken cancellationToken = default);
}
