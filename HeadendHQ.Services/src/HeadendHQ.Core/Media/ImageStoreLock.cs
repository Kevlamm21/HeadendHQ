namespace HeadendHQ.Core.Media.CommandHandlers;

internal static class ImageStoreLock
{
    public static readonly SemaphoreSlim Gate = new(1, 1);
}
